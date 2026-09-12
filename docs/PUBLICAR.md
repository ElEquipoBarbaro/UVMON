# Publicar UVMON

El juego se compila y se publica solo. Cada push a `master` genera una version
nueva de WebGL y la sube a GitHub Pages:

**https://elequipobarbaro.github.io/UVMON/**

No hay que compilar nada a mano ni subir archivos a ningun lado.

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

## Otros lugares donde publicar

GitHub Pages es gratis, no pide cuenta extra y ya esta integrado. Si en algun
momento se quiere ademas subir a **itch.io** (util para compartir el juego
fuera de la U, y maneja versiones y descargas de escritorio), se agrega un
paso con `butler` al final del workflow. Los dos destinos pueden convivir.
