# Publicar UVMON

El juego se compila y se publica solo. Cada push a `master` genera una version
nueva de WebGL y la sube a dos lugares:

- **GitHub Pages** — https://elequipobarbaro.github.io/UVMON/
- **itch.io** — `https://<usuario>.itch.io/<juego>` (ver la seccion de itch.io)

No hay que compilar nada a mano ni subir archivos a ningun lado. Se compila una
sola vez y ese mismo build va a los dos destinos.

---

## Configuracion inicial (una sola vez)

Sin estos tres pasos el workflow falla. Los hace una persona con permisos de
admin en el repo.

### 1. Conseguir la licencia de Unity

GitHub necesita una licencia para poder abrir Unity en sus servidores.

1. En el repo: pestana **Actions** -> workflow **"Activar licencia de Unity"**
   -> boton **Run workflow**.
2. Cuando termine, al final de la pagina hay un artefacto llamado
   `licencia-unity-alf`. Descargarlo y descomprimirlo: adentro viene un
   archivo `.alf`.
3. Entrar a **https://license.unity3d.com/manual**, subir ese `.alf`,
   elegir **Unity Personal** -> *"I don't use Unity in a professional capacity"*,
   y descargar el archivo `.ulf` que devuelve.

### 2. Guardar los secrets

En **Settings -> Secrets and variables -> Actions -> New repository secret**,
crear estos tres:

| Nombre           | Valor                                                        |
|------------------|--------------------------------------------------------------|
| `UNITY_LICENSE`  | El contenido **completo** del archivo `.ulf` (abrirlo con un editor de texto y copiar todo, incluidas las lineas de XML) |
| `UNITY_EMAIL`    | El correo de la cuenta de Unity                              |
| `UNITY_PASSWORD` | La contrasena de esa cuenta                                  |

### 3. Encender GitHub Pages

En **Settings -> Pages -> Build and deployment -> Source**, elegir
**GitHub Actions** (no "Deploy from a branch").

---

## Uso normal

Una vez configurado, no hay nada que hacer: se mergea a `master` y en unos
minutos la version nueva esta publicada.

Para lanzar un build a mano (sin esperar un push): **Actions** ->
**"Build y publicar (WebGL)"** -> **Run workflow**.

El progreso y los errores se ven en la pestana **Actions**.

---

## Como se numeran las versiones

Cada build se numera solo con `1.0.<numero de ejecucion>`: `1.0.1`, `1.0.2`,
`1.0.3`... No hay que tocar nada. Para cambiar el `1.0`, se edita el campo
`version` en `.github/workflows/build-webgl.yml`.

---

## Detalles que conviene saber

**El primer build tarda.** Entre 20 y 40 minutos, porque Unity importa todos
los assets desde cero. Los siguientes bajan bastante gracias al cache de
`Library`.

**Compresion.** El proyecto usa Brotli con *Decompression Fallback* activado
(`ProjectSettings.asset`). Esto es necesario: GitHub Pages no envia el header
`Content-Encoding: br`, asi que sin el fallback el juego queda en la pantalla
de carga para siempre. Si algun dia se cambia de hosting a uno que si mande
ese header (Netlify, Vercel, itch.io), se puede apagar el fallback y el juego
carga mas rapido.

**Escenas.** Se publica lo que este marcado en *File -> Build Settings*.
Hoy son, en orden: `MainMenu` (la primera es la que arranca), `Game`,
`SampleScene`, `jardinconocimiento`, `UVG_Classroom`, `jardinDefinitivo`.
Si se agrega una escena nueva hay que marcarla ahi, si no, no entra al build.

**Espacio en disco.** El workflow borra .NET, Android y otras cosas del
runner antes de empezar. No es opcional: la imagen de Unity pesa ~15 GB y sin
eso el build falla por falta de espacio.

---

## Publicar en itch.io

Ademas de GitHub Pages, cada build se sube solo a itch.io. Mientras itch.io no
este configurado ese paso se salta y el resto del workflow sigue funcionando
igual, asi que no hace falta apurarse.

### Configuracion (una sola vez)

**1. Crear el proyecto en itch.io** — en https://itch.io/game/new

- **Kind of project**: `HTML`. Es lo importante: con cualquier otro valor el
  juego no se puede jugar en el navegador, queda solo como descarga.
- Anotar el *URL slug*, o sea el `uvmon` de `https://usuario.itch.io/uvmon`.
- Se puede dejar en **Draft** hasta que este listo para mostrarlo.

**2. Sacar la API key** — en https://itch.io/user/settings/api-keys ->
*Generate new API key* -> copiar el valor.

**3. Guardar la key** — Settings -> Secrets and variables -> Actions ->
pestana **Secrets** -> New repository secret:

| Nombre           | Valor                 |
|------------------|-----------------------|
| `BUTLER_API_KEY` | La API key de itch.io |

**4. Decir a donde subir** — misma pagina, pero pestana **Variables** ->
New repository variable. Son dos:

| Nombre      | Valor                                            |
|-------------|--------------------------------------------------|
| `ITCH_USER` | El usuario de itch.io (el de `usuario.itch.io`)  |
| `ITCH_GAME` | El slug del proyecto (por ejemplo `uvmon`)       |

Van en **Variables** y no en Secrets a proposito: no son datos sensibles, y el
workflow las lee para saber si itch.io ya quedo configurado o si tiene que
saltarse ese paso.

### Uso normal

Cada build sube una version al canal `html5`, numerada igual que en Pages
(`1.0.1`, `1.0.2`...). itch.io guarda el historial completo, asi que desde el
panel del juego se puede volver a una version anterior.

La primera vez, en la pagina del proyecto hay que marcar el archivo subido como
**"This file will be played in the browser"** y ajustar el tamano del visor
(el juego usa la resolucion que tenga el Player Settings de Unity).

---

## Si el build falla

Los errores salen en la pestana **Actions**, en el paso que aparece en rojo.

**`Missing Unity License File and no Serial was found`**
Falta el secret `UNITY_LICENSE`: nadie hizo todavia la activacion de la
licencia (pasos 1 y 2 de "Configuracion inicial"). Es el error que aparece si
se mergea el workflow sin configurar nada.

**`Falta el secret BUTLER_API_KEY`**
Se configuraron las variables `ITCH_USER`/`ITCH_GAME` pero no la API key.

**El juego carga para siempre en el navegador**
Se apago `webGLDecompressionFallback` en ProjectSettings. Ver la seccion de
compresion mas arriba.
