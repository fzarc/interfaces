# Defensa — Assignment 4 (AI & Cybersecurity Defense Academy)

**Autor:** Fernando Capla
**Asignatura:** Development of Collaborative Environments
**Grado:** Ingeniería Informática y Tecnologías Virtuales · Curso 2025/2026
**Plataforma:** Moodle 4.4.12 (PHP 8.1–8.3, MariaDB ≥ 10.6.7, Apache 2)

---

## 0. Visión general del proyecto

Se ha desarrollado, sobre una instalación local de Moodle 4.4.12, una
academia ficticia llamada **AI & Cybersecurity Defense Academy
(AICDA)**. La entrega cubre seis ejercicios:

| Ej. | Entregable | Tipo | Pts |
|---|---|---|---|
| 1 | Administración del sitio + contenido (categorías, cursos, cohorts, usuarios, matrículas) | Configuración + SQL | 2 |
| 2 | `block_cohort_welcome` — saludo personalizado por cohort | Plugin (block) | 1.5 |
| 3 | `block_last_login` — últimos 10 logins más antiguos | Plugin (block) | 1.5 |
| 4 | `report_usercohorts` — usuarios agrupados por cohort | Plugin (report) | 1.5 |
| 5 | `report_categoryreport` — categorías, cursos y estudiantes | Plugin (report) | 1.5 |
| 6 | `theme_aicda` — theme custom (fork de Moove) | Plugin (theme) | 2 |

Toda la lógica nueva vive en **5 plugins custom**. El core de Moodle no
se ha modificado. Los plugins siguen la convención
`component == nombre de la carpeta` y todas las cadenas pasan por
`get_string()` con traducciones EN + IT.

### Estructura del repositorio

```
moodle/
├── blocks/
│   ├── cohort_welcome/         ← Ej. 2
│   └── last_login/             ← Ej. 3
├── report/
│   ├── usercohorts/            ← Ej. 4
│   └── categoryreport/         ← Ej. 5
├── theme/aicda/                ← Ej. 6
├── docs/
│   ├── moodle_dump.sql         ← Dump completo de la BD
│   ├── users_aicda.csv         ← CSV usado en "Upload users" (108 filas)
│   ├── setup_enrolments.sql    ← Script SQL de matrículas
│   └── DEFENSA.md              ← Este documento
└── ASSIGNMENT.md               ← Documento principal de entrega
```

---

## 1. Ejercicio 1 — Administración del sitio y contenido (2 pts)

### Qué se configuró desde la UI de Moodle

1. **Site name:** `AI&CDA` (Site administration → General settings).
2. **5 categorías de cursos** (con sortorder controlado):
   - Artificial Intelligence Fundamentals
   - Cybersecurity Defense
   - Digital Forensics
   - Professional Skills
   - Intensive Certifications
3. **17 cursos** distribuidos entre las 5 categorías.
4. **6 cohorts** a nivel sistema con sus `idnumber` exactos:

| Cohort | idnumber |
|---|---|
| Academy Directors | `directors` |
| Administrative Staff | `admin_staff` |
| Instructors | `instructors` |
| Artificial Intelligence Students | `ai_students` |
| Cybersecurity Students | `cyber_students` |
| Digital Forensics Students | `forensics_students` |

5. **108 usuarios** subidos vía `Users → Upload users` con el CSV
   `docs/users_aicda.csv`. La columna `cohort1` del CSV usa los
   `idnumber` anteriores, de modo que Moodle asigna automáticamente
   cada usuario a su cohort durante la importación.

### Por qué los `idnumber` son críticos

El CSV vincula usuario → cohort **por `idnumber`**, no por nombre. Si
los `idnumber` no coinciden exactamente, Moodle marca los usuarios como
"sin cohort" y la cascada posterior (cohort sync) falla.

### Reglas de matrícula

- **Cohort sync** (categorías AI / Cyber / Forensics): cada estudiante
  del cohort entra automáticamente como *Student* en los cursos de su
  categoría. Esto se hizo añadiendo una *instance* de `enrol_cohort`
  por curso, apuntando al cohort correspondiente.
- **Self enrolment** (Professional Skills + Intensive Certifications):
  cualquier usuario logado puede auto-matricularse. Se reactivó la
  *instance* de `enrol_self` que Moodle crea por defecto.

### El script `docs/setup_enrolments.sql`

Como en el entorno local no se podía ejecutar `sudo -u www-data php`
(la cuenta `www-data` requiere contraseña), se replicó la lógica de la
API de Moodle directamente en SQL. El script:

1. Inserta una *instance* `enrol_cohort` (con role *Student* y
   `customint1 = cohort.id`) en cada curso de las categorías 5, 6, 7.
2. Activa (`status = 0`) las *instances* de `enrol_self` ya presentes
   en las categorías 8, 9.
3. Pobla `mdl_user_enrolments` con un registro por (cohort_member,
   curso), evitando duplicados con `NOT EXISTS`.
4. Inserta los `role_assignments` correspondientes con
   `component = 'enrol_cohort'`.

El script es **idempotente**: re-ejecutarlo no genera duplicados
gracias a los `NOT EXISTS`. Esto replica exactamente lo que haría
`enrol_cohort_handler::sync()` en la API de Moodle.

> **Por qué SQL directo en lugar de un script PHP CLI:** En el entorno
> local no era posible invocar la CLI de Moodle como `www-data`. El
> script PHP equivalente (`docs/setup_enrolments.php`) está incluido en
> el repositorio como referencia, pero la ejecución real se hizo con
> SQL para no depender del usuario del sistema.

---

## 2. Ejercicio 2 — `block_cohort_welcome` (1.5 pts)

### Funcionalidad

Bloque que:
1. Saluda al usuario por su nombre completo.
2. Muestra un mensaje específico de su cohort (uno por cohort).
3. Indica cuántos miembros tiene ese cohort.
4. Si el usuario no está logado o no tiene cohort, muestra un mensaje
   alternativo.

### Estructura del plugin

```
blocks/cohort_welcome/
├── block_cohort_welcome.php       ← Clase principal del bloque
├── version.php                    ← Versión y dependencias
├── db/access.php                  ← Capabilities
├── lang/en/block_cohort_welcome.php
├── lang/it/block_cohort_welcome.php
└── classes/privacy/provider.php   ← Privacy API (null provider)
```

### Cómo lo encuentra Moodle

La convención del API de bloques es:
- Carpeta `blocks/cohort_welcome/`.
- Clase `block_cohort_welcome` que extiende `block_base`.
- Component en `version.php`: `block_cohort_welcome`.

Cuando se entra como admin a *Site administration → Notifications*,
Moodle detecta el plugin nuevo, ejecuta la instalación y lo añade al
catálogo de "Add a block".

### Código explicado — `block_cohort_welcome.php`

```php
class block_cohort_welcome extends block_base {

    public function init() {
        $this->title = get_string('pluginname', 'block_cohort_welcome');
    }
```
- `init()` lo invoca Moodle al construir el bloque. Solo fija el
  título; el `pluginname` se localiza vía `get_string()`.

```php
    public function get_content() {
        global $USER, $DB;

        if ($this->content !== null) {
            return $this->content;
        }
        $this->content = new stdClass();
        $this->content->text = '';
        $this->content->footer = '';
```
- `get_content()` es el método obligatorio del API de bloques.
- El primer `if` es un patrón estándar de cache: si Moodle ya pidió el
  contenido en esta petición, no lo recalculamos.

```php
        if (!isloggedin() || isguestuser()) {
            $this->content->text = html_writer::tag(
                'p',
                get_string('welcomeguest', 'block_cohort_welcome')
            );
            return $this->content;
        }
```
- Si el visitante no está logado o es invitado, se muestra el saludo
  genérico. Esto cubre la portada cuando hay logout.

```php
        $fullname = fullname($USER);
        $text  = html_writer::tag('h4',
            get_string('hello', 'block_cohort_welcome', $fullname));
```
- `fullname($USER)` respeta la configuración `fullnamedisplay` del
  sitio (orden firstname/lastname, ocultación, etc.).
- `get_string('hello', 'block_cohort_welcome', $fullname)` inyecta el
  nombre en `'Hello, {$a}!'` (EN) o `'Ciao, {$a}!'` (IT).

```php
        $memberships = $DB->get_records_select(
            'cohort_members',
            'userid = ?',
            [$USER->id],
            'id'
        );
```
- Consulta a la tabla `cohort_members` del DML de Moodle. Devuelve
  todas las cohort memberships del usuario. Se usa `get_records_select`
  con placeholders posicionales (`?`) por seguridad SQL.

```php
        foreach ($memberships as $membership) {
            $cohort = $DB->get_record_select('cohort',
                'id = ?', [$membership->cohortid]);
            if (!$cohort) { continue; }
            $members = $DB->get_records_select('cohort_members',
                'cohortid = ?', [$cohort->id]);
```
- Por cada membership, se carga el cohort y se cuentan sus miembros.

```php
            $stringkey = 'msg_' . $cohort->idnumber;
            if (!empty($cohort->idnumber) &&
                get_string_manager()->string_exists($stringkey, 'block_cohort_welcome')) {
                $cohortmsg = get_string($stringkey, 'block_cohort_welcome');
            } else {
                $cohortmsg = get_string('msg_default',
                    'block_cohort_welcome', format_string($cohort->name));
            }
```
- **Aquí está el truco del bloque:** el mensaje específico se busca
  por la clave `msg_<idnumber>`. Por ejemplo, para el cohort
  `ai_students` el mensaje vive bajo la clave `msg_ai_students` en el
  fichero de idiomas. Si el cohort no tiene un mensaje propio, se cae
  al `msg_default` genérico.
- `get_string_manager()->string_exists()` comprueba que la clave existe
  **antes** de llamar a `get_string()`, evitando el warning `[[ ]]`
  típico de Moodle cuando una clave no está traducida.

```php
            $a = new stdClass();
            $a->cohort = format_string($cohort->name);
            $a->count  = count($members);
            $text .= html_writer::tag('p', $cohortmsg);
            $text .= html_writer::tag('p',
                get_string('cohortmembers', 'block_cohort_welcome', $a),
                ['class' => 'cohort-welcome-members']);
        }
```
- `format_string()` aplica filtros y escapa el nombre del cohort
  (recomendación oficial de Moodle para todo texto que provenga de la
  BD y vaya al HTML).
- El placeholder `{$a->cohort}` y `{$a->count}` aparecen en la cadena
  `cohortmembers` ("You are one of {$a->count} members of {$a->cohort}.").

### Capabilities (`db/access.php`)

Solo se declaran las dos capabilities estándar de cualquier bloque:
- `block/cohort_welcome:addinstance` — permite añadir el bloque en
  cualquier contexto (asignada a *editingteacher* y *manager*).
- `block/cohort_welcome:myaddinstance` — permite añadirlo al
  Dashboard del usuario.

### Tabla de mensajes por cohort (en `lang/en`)

| Clave | Mensaje (EN) |
|---|---|
| `msg_directors` | As an Academy Director, you set the strategic direction... |
| `msg_admin_staff` | From Administration you keep the academy running smoothly... |
| `msg_instructors` | As an Instructor at AICDA, you train the next generation... |
| `msg_ai_students` | Welcome, AI Student! You are learning to build and audit... |
| `msg_cyber_students` | Welcome, Cybersecurity Student! Every concept you master... |
| `msg_forensics_students` | Welcome, Digital Forensics Student! You are learning to follow... |

`lang/it` traduce las mismas claves al italiano.

---

## 3. Ejercicio 3 — `block_last_login` (1.5 pts)

### Funcionalidad

Bloque que lista a los **10 usuarios cuyo último acceso es el más
antiguo**, mostrando:
- Nombre completo (enlace al perfil).
- Tiempo transcurrido desde el último acceso (en formato humano y
  localizado: *3 hours, 25 mins ago* / *3 ore 25 min fa*).

### Código clave — `block_last_login.php`

```php
$users = $DB->get_records_select(
    'user',
    'lastaccess > 0 AND deleted = 0 AND id != 1',
    [],
    'lastaccess ASC',
    '*',
    0,
    10
);
```

- `lastaccess > 0`: excluye usuarios que nunca han entrado (los que
  tienen `lastaccess = 0` no son "antiguos", simplemente no han
  accedido nunca).
- `deleted = 0`: excluye usuarios borrados (Moodle hace soft-delete).
- `id != 1`: excluye al usuario invitado (id=1).
- `ORDER BY lastaccess ASC LIMIT 10`: orden ascendente → los más
  antiguos primero. Equivale a "los que llevan más tiempo sin
  conectarse".

```php
$now = time();
foreach ($users as $user) {
    $elapsed = $now - $user->lastaccess;
    $url = new moodle_url('/user/profile.php', ['id' => $user->id]);
    $namelink = html_writer::link($url, fullname($user));

    $a = new stdClass();
    $a->name = $namelink;
    $a->time = format_time($elapsed);
    ...
}
```

- `format_time($segundos)` es la función estándar de Moodle que
  convierte segundos a "3 hours 25 mins" automáticamente localizado.
  No hace falta `gettype($a)` ni formatear manualmente; basta con
  pasarle un delta y Moodle elige las unidades adecuadas según el
  idioma del usuario.
- La cadena `userline` es `'{$a->name} — {$a->time} ago'` en EN y la
  versión equivalente en IT.

### Lang IT

```
$string['userline'] = '{$a->name} — {$a->time} fa';
```

`format_time()` ya devuelve "3 ore 25 min" si el usuario tiene la
interfaz en italiano, así que solo hay que traducir la palabra "ago".

---

## 4. Ejercicio 4 — `report_usercohorts` (1.5 pts)

### Funcionalidad

Informe accesible desde *Site administration → Reports → Users by
cohort*. Para cada cohort, se renderiza una tabla con todos sus
miembros, mostrando:

| Columna | Contenido |
|---|---|
| Foto | `$OUTPUT->user_picture()` (35 px) |
| Nombre completo | Enlace a `/user/profile.php?id=...` |
| Email | `s($user->email)` (escapado) |
| Cursos matriculados | Conteo `DISTINCT` de `user_enrolments` |

### Estructura

```
report/usercohorts/
├── index.php              ← Página del informe
├── settings.php           ← Registro en menú "Reports"
├── version.php
├── db/access.php          ← Capability custom
└── lang/{en,it}/report_usercohorts.php
```

### Cómo se integra en Moodle

```php
// settings.php
$ADMIN->add(
    'reports',
    new admin_externalpage(
        'reportusercohorts',
        get_string('pluginname', 'report_usercohorts'),
        "$CFG->wwwroot/report/usercohorts/index.php",
        'report/usercohorts:view'
    )
);
```

- Se inscribe como `admin_externalpage` bajo la rama `reports`.
- El último argumento es la capability que limita el acceso.
- En `db/access.php` se define `report/usercohorts:view` con
  `archetypes => ['manager' => CAP_ALLOW]`, de modo que solo el rol
  *Manager* (y el admin) lo ve.

### Código clave — conteo de cursos

```php
$coursecount = $DB->count_records_sql(
    "SELECT COUNT(DISTINCT e.courseid)
       FROM {user_enrolments} ue
       JOIN {enrol} e ON e.id = ue.enrolid
      WHERE ue.userid = ?",
    [$user->id]
);
```

- Se cuentan cursos **distintos**: un usuario puede tener varias
  *instances* de matrícula en el mismo curso (p. ej. cohort + self) y
  no queremos contarlas dos veces.
- Las llaves `{user_enrolments}` son la sintaxis DML de Moodle: se
  expanden a `mdl_user_enrolments` (con el prefijo de tablas
  configurado).

### Por qué `admin_externalpage_setup`

```php
admin_externalpage_setup('reportusercohorts', '', null, '',
    ['pagelayout' => 'report']);
```

- Marca la página como página del admin (verifica la capability
  declarada en `settings.php`, configura breadcrumbs y `$PAGE`).
- `pagelayout => 'report'` da el layout estándar de los reports
  (cabecera, footer, ancho completo).

### Capability

```php
$capabilities = [
    'report/usercohorts:view' => [
        'riskbitmask' => RISK_PERSONAL,
        'captype' => 'read',
        'contextlevel' => CONTEXT_SYSTEM,
        'archetypes' => ['manager' => CAP_ALLOW],
    ],
];
```

- `RISK_PERSONAL`: el informe lista emails de usuarios → Moodle lo
  marca con el icono de riesgo correspondiente en la pantalla de
  permisos.
- `contextlevel = CONTEXT_SYSTEM`: la capability se asigna a nivel
  sistema (no por curso).

---

## 5. Ejercicio 5 — `report_categoryreport` (1.5 pts)

### Funcionalidad

Para cada categoría de cursos, se renderiza una tabla con:

| Columna | Contenido |
|---|---|
| Course | Nombre enlazado a `/course/view.php?id=...` |
| Description | Resumen del curso renderizado con `format_text()` |
| Start date / End date | Fechas formateadas según locale |
| Duration | `format_time(end − start)` localizado |
| Students | Nº de usuarios con role *Student* en el contexto del curso |
| Courses with same student count | **Cuántos cursos de la misma categoría tienen el mismo nº de estudiantes (incluyendo el propio)** |

### Código clave — `index.php`

```php
$datefmt = get_string('strftimedatefullshort', 'core_langconfig');
$studentrole = $DB->get_record('role',
    ['shortname' => 'student'], '*', MUST_EXIST);
```
- Se cachea el formato de fecha localizado y el role *Student* (que
  podría tener distinto `id` en distintas instalaciones — por eso se
  busca por `shortname`).

```php
$studentcounts = $DB->get_records_sql(
    "SELECT c.id, COUNT(DISTINCT ra.userid) AS students
       FROM {course} c
       JOIN {context} ctx ON ctx.instanceid = c.id
         AND ctx.contextlevel = :ctxlevel
       LEFT JOIN {role_assignments} ra ON ra.contextid = ctx.id
         AND ra.roleid = :roleid
      WHERE c.category = :catid
      GROUP BY c.id",
    [
        'ctxlevel' => CONTEXT_COURSE,
        'roleid'   => $studentrole->id,
        'catid'    => $category->id,
    ]
);
```

- Se calcula el conteo de estudiantes de **todos los cursos de la
  categoría en una sola query** (en lugar de N queries — patrón
  N+1 evitado).
- `LEFT JOIN` permite que aparezcan en el resultado cursos con 0
  estudiantes (`students = 0`), no se filtran.
- Se cuentan `DISTINCT ra.userid` porque un usuario puede tener varios
  `role_assignments` con role student en el mismo curso (vía cohort y
  vía manual, por ejemplo).
- La consulta cuenta a través de la tabla `context`: en Moodle, un
  usuario "es estudiante de un curso" si tiene un `role_assignment`
  con el role *Student* en el `context_course` de ese curso.

```php
$tally = [];
foreach ($studentcounts as $row) {
    $tally[(int)$row->students] = ($tally[(int)$row->students] ?? 0) + 1;
}
```

- Se construye un histograma: `tally[N] = cuántos cursos de esta
  categoría tienen exactamente N estudiantes`.
- Luego, dentro del foreach de cursos, basta con `tally[$students]`
  para obtener la última columna **en O(1)** sin volver a la BD.

```php
foreach ($courses as $course) {
    $ctx = context_course::instance($course->id);

    $description = !empty($course->summary)
        ? format_text($course->summary, $course->summaryformat,
            ['context' => $ctx])
        : html_writer::tag('em',
            get_string('nodesc', 'report_categoryreport'));

    $start = !empty($course->startdate)
        ? userdate($course->startdate, $datefmt) : '—';
    $end   = !empty($course->enddate)
        ? userdate($course->enddate,   $datefmt) : '—';

    if (!empty($course->startdate) && !empty($course->enddate)
        && $course->enddate > $course->startdate) {
        $duration = format_time($course->enddate - $course->startdate);
    } else {
        $duration = '—';
    }

    $students = isset($studentcounts[$course->id])
        ? (int)$studentcounts[$course->id]->students : 0;
    $same = $tally[$students] ?? 0;
    ...
}
```

- `format_text()` con `context` aplica filtros, embeds, escape HTML y
  permisos del contexto del curso (recomendado por Moodle para
  cualquier texto multi-formato).
- `userdate()` formatea timestamps según la zona horaria y locale del
  usuario.
- `format_time()` para la duración: convierte segundos en "2 months 3
  weeks" automáticamente.
- El conteo de cursos "con el mismo número de estudiantes" **incluye
  al propio curso**: si la categoría tiene 4 cursos con 25 estudiantes
  cada uno, la columna mostrará "4" para todos ellos. Esta interpretación
  cuadra con el enunciado ("how many courses in this category have the
  same number of students as this one").

### Cabecera de cada tabla

```php
echo $OUTPUT->heading(
    get_string('categoryheading', 'report_categoryreport', $heading), 3
);
```

Donde la cadena es `'{$a->name} — {$a->count} courses'`, p.ej.
"Cybersecurity Defense — 4 courses".

---

## 6. Ejercicio 6 — `theme_aicda` (2 pts)

### Decisión de diseño: fork de Moove en lugar de child de Boost

Se ha clonado **Moove 4.4.3** y se ha hecho un `find-replace`
`moove → aicda` completo, incluyendo:
- Component name (`theme_moove` → `theme_aicda`).
- Todos los `get_string()` namespaces.
- Templates Mustache (`{{# str }} foo, theme_moove {{/ str }} →
  theme_aicda`).
- AMD JS namespace.
- Sourcemaps generados por Grunt.

### Por qué un fork en lugar de un child theme

El enunciado pide un theme "personalizado de Moodle", no una
configuración. Un child theme habría sido un único archivo `scss`
heredando del padre — insuficiente para mostrar comprensión del API de
themes. Al hacer fork se demuestra que se entiende:
- Estructura `config.php` (parent, sheets, layouts, SCSS callback).
- Layouts (carpeta `layout/` con plantillas para diferentes vistas).
- Renderers (en `classes/output/`).
- Pipeline de SCSS (Gruntfile, `style/` generado).

### Licencia y atribución (cumplimiento GPL)

Moove está bajo GPL v3 → al hacer fork, se mantiene la misma licencia
y la **atribución a Willian Mano** (autor original) en todos los
ficheros `version.php`, `config.php`, etc.

```php
// version.php
/**
 * AICDA theme — AI & Cybersecurity Defense Academy.
 *
 * Forked from Moove (theme_moove) by Willian Mano under GPL v3+.
 *
 * @copyright  2026 Fernando Capla (fork);
 *             originally 2022 Willian Mano - https://conecti.me
 * @license    http://www.gnu.org/copyleft/gpl.html GNU GPL v3 or later
 */

$plugin->component   = 'theme_aicda';
$plugin->version     = 2026052400;
$plugin->release     = '1.0.0';
$plugin->maturity    = MATURITY_STABLE;
$plugin->requires    = 2024041600;       // Moodle 4.4
$plugin->dependencies = ['theme_boost' => 2024042200];
```

### Idiomas

- `lang/en/theme_aicda.php`: copia completa del fichero original con
  el componente renombrado.
- `lang/it/theme_aicda.php`: solo se traducen las cadenas user-facing
  (configsdescription, custombrand, etc.). El resto cae automáticamente
  al EN porque Moodle resuelve cadenas faltantes con fallback al
  idioma por defecto.

### Configuración UI pendiente para la defensa

- Activar el theme: *Site administration → Appearance → Theme
  selector → Default → Change theme → AICDA*.
- Logo, colors, sliders y footer: *Site administration → Appearance →
  AICDA → General / Header / Slideshow / Footer*.
- HTML blocks en la portada con los anuncios de cursos.

---

## 7. Decisiones técnicas relevantes

### 7.1. Por qué `get_records_select` con placeholders

Todas las queries usan placeholders (`?` o `:nombre`) en lugar de
concatenar strings. Esto:
- Previene SQL injection.
- Aprovecha el cache de plan de la BD (mismas queries reutilizan plan).
- Es la práctica recomendada en el coding style de Moodle.

### 7.2. Por qué `format_string` / `format_text` / `s()`

- `format_string()` → para texto plano que va al HTML (nombres de
  cursos, cohorts). Aplica filtros y escape básico.
- `format_text()` → para texto rich (descripciones de cursos). Aplica
  filtros multimedia, embeds, sanitización completa según el contexto.
- `s()` → escape HTML simple (emails, valores que no deben filtrarse).

No usar estas funciones es la causa #1 de fallos XSS en plugins de
Moodle.

### 7.3. Privacy API

Cada plugin tiene una clase `classes/privacy/provider.php` que
implementa `\core_privacy\local\metadata\null_provider` con un string
`privacy:metadata` que declara: "este plugin no almacena datos
personales propios; solo muestra datos existentes". Esto es
**obligatorio desde Moodle 3.4** para superar la herramienta `Site
administration → Privacy → Data registry`.

### 7.4. Versionado de plugins

Todos los plugins llevan:
```
$plugin->version  = 2026052400;     // YYYYMMDDXX
$plugin->requires = 2024041600;     // Moodle 4.4
$plugin->maturity = MATURITY_STABLE;
$plugin->release  = '1.0.0';
```

Moodle compara `$plugin->version` con la versión instalada en
`mdl_config_plugins` para decidir si correr upgrades.

---

## 8. Cómo reproducir la instalación

```bash
# 1. Clonar
cd /var/www/html
git clone https://github.com/fzarc/moodle-aicda moodle
cd moodle
sudo chown -R www-data:www-data .

# 2. moodledata
sudo mkdir -p /var/www/moodledata
sudo chown -R www-data:www-data /var/www/moodledata
sudo chmod -R 777 /var/www/moodledata

# 3. Base de datos
mysql -uroot -proot \
  -e "CREATE DATABASE IF NOT EXISTS moodle DEFAULT CHARACTER SET utf8mb4 \
      COLLATE utf8mb4_unicode_ci;"
mysql -uroot -proot moodle < docs/moodle_dump.sql

# 4. Config
cp config-sample.php config.php
sudo chown www-data:www-data config.php
sudo chmod 640 config.php

# 5. Acceder
xdg-open http://localhost/moodle
```

Login admin: `admin` / *(contraseña configurada en la instalación)*.
Usuarios de prueba: ver `docs/users_aicda.csv` (contraseña común
`AiCda2026!` antes del hash, todas en formato Moodle bcrypt).

---

## 9. Preguntas previsibles del tribunal y respuestas

**P: ¿Por qué los bloques usan `format_string` y los reports `format_text`?**
R: `format_string` es para texto plano de una línea (nombre de cohort,
nombre de curso). `format_text` aplica filtros multimedia y permite
HTML enriquecido — apropiado para descripciones de curso que pueden
llevar `<p>`, `<img>`, etc. Pasarles datos del revés sería una
vulnerabilidad XSS o un texto sobre-escapado.

**P: ¿Qué pasa si añadimos un cohort nuevo sin `idnumber`?**
R: El bloque `cohort_welcome` cae al `msg_default` ("Welcome to
<nombre del cohort>"). No rompe. Está cubierto por el `!empty($cohort->idnumber)`
y `string_exists()`.

**P: ¿Por qué `id != 1` en el bloque `last_login`?**
R: En Moodle el `id=1` es el usuario *Guest* (invitado), que tiene
`lastaccess > 0` cada vez que alguien entra como invitado. Si no se
excluye, "Guest" siempre aparecería en la lista, contaminando el
ranking de usuarios reales con menos actividad.

**P: ¿Y el admin? ¿También se excluye?**
R: No. El admin (`id=2` por defecto) es un usuario válido y, si lleva
tiempo sin entrar, debería aparecer.

**P: ¿Cómo se cuentan cursos "matriculados" en el report `usercohorts`?**
R: `COUNT(DISTINCT e.courseid) FROM user_enrolments JOIN enrol`. Es la
forma "low-level" pero exacta: cuenta cursos distintos donde el
usuario tiene al menos una matriculación, ignorando si es cohort,
self, manual, etc. El `DISTINCT` es clave: si un usuario está
matriculado vía cohort *y* vía self en el mismo curso, cuenta 1.

**P: ¿Por qué no usar `enrol_get_users_courses($userid)`?**
R: Habría funcionado, pero al cargar 100+ usuarios sería N+1 (una
llamada por usuario, cada una con su propia query interna). La query
SQL directa es más eficiente y suficiente para la métrica pedida.

**P: ¿La columna "courses with same student count" incluye el propio curso?**
R: Sí. Si la categoría tiene 4 cursos con 25 estudiantes cada uno, los
4 muestran "4". El enunciado dice *"how many courses ... have the same
number"*, e interpreto que "tener el mismo número" es una propiedad
del valor, no una exclusión.

**P: ¿Por qué un fork de Moove en lugar de un child theme?**
R: Demuestra comprensión del API de themes (config, layouts, SCSS,
renderers). Un child theme es un único `scss` heredando del padre —
no muestra trabajo de plugin development. El fork mantiene la
atribución original y la licencia GPL.

**P: ¿Cómo se garantiza que la instalación sea reproducible?**
R: El dump completo de la BD (`docs/moodle_dump.sql`) más el
`config-sample.php` reproducen el estado exacto del sitio. El CSV de
usuarios y el SQL de matrículas son referencias que **no hacen falta
re-ejecutar** porque el estado ya está en el dump; están como
documentación del proceso seguido.

**P: ¿Por qué no se modifica el core de Moodle?**
R: Modificar el core rompe las actualizaciones de Moodle y no es
necesario: el API de plugins (blocks, reports, themes) cubre el 100%
de los casos del assignment. Es la práctica oficial recomendada.

**P: ¿Por qué SQL directo para las matrículas en lugar de la API de Moodle?**
R: Constraint del entorno local: `sudo -u www-data php cli/...`
requería contraseña, así que no se podía ejecutar la API desde CLI. El
SQL replica fielmente lo que la API (`enrol_cohort_handler::sync`)
hace, y es idempotente (re-ejecutarlo no genera duplicados). Para
producción se usaría la API.

**P: ¿Qué pasa si quiero traducir las cadenas a un tercer idioma?**
R: Basta crear `blocks/cohort_welcome/lang/es/block_cohort_welcome.php`
con las mismas claves traducidas. Moodle resuelve el idioma usando la
preferencia del usuario y, si una clave falta, cae al EN.

---

## 10. Resumen ejecutivo

- 5 plugins custom: `block_cohort_welcome`, `block_last_login`,
  `report_usercohorts`, `report_categoryreport`, `theme_aicda`.
- 100% en EN + IT.
- Core sin modificar.
- Seguridad: placeholders en queries, `format_string`/`format_text`,
  capabilities con riesgos declarados, Privacy API implementada.
- Reproducible: dump SQL + CSV + script de matrículas + `config-sample.php`.
- Entregado en repo privado `github.com/fzarc/moodle-aicda` (privado
  porque el dump contiene hashes de contraseñas y emails de 108
  usuarios).
