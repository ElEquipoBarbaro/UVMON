

## 2. Mecánica vigente de captura

La captura se activa automáticamente después de derrotar a un UVGmon configurado como capturable.

La mecánica vigente usa:

- Un único círculo indicador.
- Un área circular delimitada de captura.
- Movimiento aleatorio y continuo del círculo dentro del área.
- Reducción progresiva del tamaño del círculo.
- Un frasco que cae cuando el jugador hace clic izquierdo.
- Una validación geométrica para determinar si el frasco cayó dentro del círculo.

La captura es exitosa cuando el punto de impacto del frasco queda dentro del círculo indicador en el instante del impacto.

La captura falla cuando el punto de impacto queda fuera del círculo o cuando se cumple una condición de fallo definida en este documento.

# RECUERDA QUE

**El sprite del frasco estan en "Assets/Items/frasco.png"**

La escena de prueba es Assets/Scenes/jardinconocimiento.unity

Usa el MCP de unity

El combate sucede en BattleUI 


Los scripts del sistema de combate estan en C:\Users\donMatthiuz\Music\UVMON\Assets\Scripts\Combat


La explicacion de cada sistema esta en 

C:\Users\donMatthiuz\Music\UVMON\Assets\Scripts\Combat\COMBAT_SYSTEM_ANALYSIS.md

C:\Users\donMatthiuz\Music\UVMON\Assets\Scripts\MenuInventary\MENU_INVENTORY_ANALYSIS.md

etc pero siempre terminan con md, sino existe lee los archivos

## 3. Convenciones y variables

Las variables de código deben utilizar nombres sin tildes:

```text
porcentajeCaptura
porcentajeAtaque
porcentajeAcertividad
danoBase
danoFinal
radioInicial
radioActual
radioMinimoPermitido
velocidadReduccion
velocidadMovimientoIndicador
posicionIndicador
posicionDestino
posicionImpactoFrasco
toleranciaImpacto
tiempoMaximoCaptura
```

Los porcentajes deben limitarse antes de utilizarse:

```text
porcentaje = clamp(porcentaje, 0, 100)
```

En Unity:

```csharp
float porcentajeLimitado = Mathf.Clamp(porcentaje, 0f, 100f);
```

---

# PARTE I — SISTEMA DE CAPTURA

## 4. Requisito del frasco

Antes de mostrar o iniciar el desafío de captura, el sistema debe comprobar:

```text
cantidadFrascos > 0
```

Si no existe un frasco:

```text
capturaExitosa = false
motivoFallo = NoJar
```

En este caso:

- No comienza el movimiento del círculo.
- No comienza la reducción del círculo.
- No se consume ningún frasco.
- No se registra el UVGmon.
- Se muestra un mensaje claro al jugador.
- El flujo de salida del combate continúa sin bloquearse.

### 4.1. Momento de consumo

La regla de consumo seleccionada es:

```text
El frasco se consume exactamente una vez
cuando el jugador realiza el primer clic válido
que inicia la caída.
```

Consecuencias:

- Abrir la interfaz no consume el frasco.
- Un intento que termina antes del primer clic no consume el frasco.
- El frasco se consume aunque la caída termine fuera del círculo.
- Los clics posteriores no consumen más frascos.
- El resultado de éxito o fallo no debe volver a descontarlo.
- El consumo debe realizarse mediante el sistema de inventario existente.
- La operación debe ser atómica.

Pseudocódigo:

```text
si estado == Active
y clicIzquierdo
y jarConsumed == false:

    si TryConsumeCaptureJar() == false:
        resolver fallo NoJar
        terminar

    jarConsumed = true
    iniciar caída
```

---

## 5. Porcentaje de captura

`porcentajeCaptura` no representa la posibilidad aleatoria de obtener al UVGmon.

Su única función es modificar la dificultad manual del desafío.

### 5.1. Efecto esperado

```text
porcentajeCaptura alto:
    círculo inicial más grande
    reducción más lenta
    dificultad menor

porcentajeCaptura bajo:
    círculo inicial más pequeño
    reducción más rápida
    dificultad mayor
```

### 5.2. Normalización

Definir:

```text
pCaptura =
    clamp(porcentajeCaptura, 0, 100) / 100
```

Por tanto:

```text
0 <= pCaptura <= 1
```

En Unity:

```csharp
float pCaptura = Mathf.Clamp01(porcentajeCaptura / 100f);
```

---

## 6. Radio inicial del círculo indicador

Usar los parámetros globales:

```text
radioInicialMinimo
radioInicialMaximo
```

Restricciones:

```text
radioInicialMaximo >= radioInicialMinimo
radioInicialMinimo > 0
```

Fórmula:

```text
radioInicial =
    radioInicialMinimo
    + pCaptura
    * (radioInicialMaximo - radioInicialMinimo)
```

Forma equivalente en Unity:

```csharp
float radioInicial = Mathf.Lerp(
    radioInicialMinimo,
    radioInicialMaximo,
    pCaptura
);
```

### 6.1. Interpretación

Cuando `pCaptura = 0`:

```text
radioInicial = radioInicialMinimo
```

Cuando `pCaptura = 1`:

```text
radioInicial = radioInicialMaximo
```

### 6.2. Conversión a tamaño visual

Si el círculo se representa mediante un `RectTransform` y `radioInicial` está expresado en unidades de Canvas:

```text
diametroInicial = 2 * radioInicial
```

En Unity:

```csharp
indicatorRect.sizeDelta = Vector2.one * (2f * radioInicial);
```

La validación geométrica debe trabajar con el radio, no con el diámetro.

---

## 7. Velocidad de reducción del círculo

Usar:

```text
velocidadReduccionMinima
velocidadReduccionMaxima
```

Restricciones:

```text
velocidadReduccionMaxima >= velocidadReduccionMinima
velocidadReduccionMinima > 0
```

Fórmula:

```text
velocidadReduccion =
    velocidadReduccionMaxima
    - pCaptura
    * (
        velocidadReduccionMaxima
        - velocidadReduccionMinima
      )
```

Forma equivalente:

```csharp
float velocidadReduccion = Mathf.Lerp(
    velocidadReduccionMaxima,
    velocidadReduccionMinima,
    pCaptura
);
```

La interpolación se realiza de máximo a mínimo porque un porcentaje alto debe producir una reducción más lenta.

### 7.1. Interpretación

Cuando `pCaptura = 0`:

```text
velocidadReduccion = velocidadReduccionMaxima
```

Cuando `pCaptura = 1`:

```text
velocidadReduccion = velocidadReduccionMinima
```

---

## 8. Radio del círculo a través del tiempo

El círculo comienza con:

```text
radioActual(0) = radioInicial
```

Mientras el intento está activo:

```text
radioActual(t) =
    max(
        radioMinimoPermitido,
        radioInicial - velocidadReduccion * t
    )
```

Actualización incremental:

```text
radioActual =
    max(
        radioMinimoPermitido,
        radioActual
        - velocidadReduccion * deltaTime
    )
```

En Unity:

```csharp
radioActual = Mathf.Max(
    radioMinimoPermitido,
    radioActual - velocidadReduccion * deltaTime
);
```

### 8.1. Tiempo escalado o no escalado

Si la batalla utiliza:

```csharp
Time.timeScale = 0f;
```

la captura debe utilizar:

```csharp
Time.unscaledDeltaTime
```

En otro caso puede utilizar `Time.deltaTime`.

La decisión debe centralizarse para que el movimiento, la reducción, el temporizador y la caída usen la misma base temporal.

### 8.2. Fallo por tamaño mínimo

Si el jugador no inició la caída y se cumple:

```text
radioActual <= radioMinimoPermitido
```

el resultado puede resolverse como:

```text
motivoFallo = IndicatorTooSmall
capturaExitosa = false
```

El resultado debe emitirse una sola vez.

---

## 9. Movimiento aleatorio del círculo

El círculo se mueve hacia destinos aleatorios dentro de un área delimitada.

El movimiento debe ser continuo. No debe teletransportarse.

### 9.1. Límites del área

Definir:

```text
limiteIzquierdo
limiteDerecho
limiteInferior
limiteSuperior
margenArea
radioActual
```

El centro del círculo debe permanecer dentro de:

```text
xMinValido =
    limiteIzquierdo
    + radioActual
    + margenArea

xMaxValido =
    limiteDerecho
    - radioActual
    - margenArea

yMinValido =
    limiteInferior
    + radioActual
    + margenArea

yMaxValido =
    limiteSuperior
    - radioActual
    - margenArea
```

Un destino aleatorio válido se genera con:

```text
xDestino = random(xMinValido, xMaxValido)
yDestino = random(yMinValido, yMaxValido)
```

```text
posicionDestino = (xDestino, yDestino)
```

Antes de generar el destino debe verificarse:

```text
xMinValido <= xMaxValido
yMinValido <= yMaxValido
```

Si no se cumple, el área es demasiado pequeña para el radio actual y debe aplicarse una salida segura:

- centrar el indicador;
- limitar el radio al máximo permitido por el área; o
- impedir el inicio y reportar una configuración inválida.

No se deben generar valores aleatorios con intervalos invertidos.

### 9.2. Movimiento suave

En cada actualización:

```text
posicionIndicador =
    MoveTowards(
        posicionIndicador,
        posicionDestino,
        velocidadMovimientoIndicador * deltaTime
    )
```

En Unity:

```csharp
posicionIndicador = Vector2.MoveTowards(
    posicionIndicador,
    posicionDestino,
    velocidadMovimientoIndicador * deltaTime
);
```

### 9.3. Cambio de destino

Cuando:

```text
Distance(posicionIndicador, posicionDestino)
<= epsilonDestino
```

se genera un nuevo destino.

Para evitar destinos casi idénticos puede utilizarse:

```text
Distance(nuevoDestino, posicionIndicador)
>= distanciaMinimaEntreDestinos
```

Debe existir un número máximo de intentos para buscar un nuevo destino, evitando bucles infinitos en áreas pequeñas.

### 9.4. Porcentaje de captura y movimiento

En la versión vigente:

```text
velocidadMovimientoIndicador
no depende de porcentajeCaptura
```

El porcentaje de captura solo modifica:

1. El radio inicial.
2. La velocidad de reducción.

La velocidad de movimiento puede balancearse globalmente o por dificultad en el futuro, pero no debe agregarse esa relación sin aprobación.

---

## 10. Control y caída del frasco

### 10.1. Regla obligatoria

El jugador utiliza clic izquierdo para activar la caída.

Solo se acepta el clic cuando:

```text
estadoCaptura == Active
```

Al aceptarlo:

```text
estadoCaptura = Dropping
```

Después:

- Se bloquean clics adicionales.
- Se consume un único frasco.
- Se fija el punto horizontal o la posición de caída.
- Comienza la animación.
- El círculo puede detenerse inmediatamente o continuar hasta el impacto, según la configuración elegida; debe usarse una sola regla de forma consistente.

### 10.2. Posicionamiento recomendado

Si el proyecto no tiene una interacción previa definida, utilizar:

- El frasco sigue horizontalmente la posición del cursor.
- Su coordenada vertical previa a la caída permanece fija.
- El movimiento horizontal se limita al área de captura.
- Al hacer clic, se bloquea la coordenada horizontal.
- El frasco cae verticalmente.
- Durante la caída no puede corregirse.
- Solo se permite una caída por intento.

Esta interacción debe confirmarse contra la implementación existente antes de crear una nueva.

### 10.3. Interpolación de caída

Definir:

```text
posicionInicioCaida
posicionFinCaida
duracionCaidaFrasco
tiempoTranscurrido
```

Progreso normalizado:

```text
u =
    clamp(
        tiempoTranscurrido / duracionCaidaFrasco,
        0,
        1
    )
```

Movimiento lineal:

```text
posicionFrasco(u) =
    Lerp(
        posicionInicioCaida,
        posicionFinCaida,
        u
    )
```

Para dar sensación de caída puede aplicarse una curva de animación configurable:

```text
uCurvo = curvaCaida(u)
```

```text
posicionFrasco =
    Lerp(
        posicionInicioCaida,
        posicionFinCaida,
        uCurvo
    )
```

La curva visual no debe alterar el punto final de impacto.

---

## 11. Punto de impacto

Debe definirse un punto estable del frasco para evaluar la captura.

Recomendación:

```text
posicionImpactoFrasco =
    centro inferior del frasco
```

o un `RectTransform` hijo llamado conceptualmente `ImpactPoint`.

No debe validarse usando el rectángulo completo del sprite si el objetivo de diseño es que el punto de caída quede dentro del círculo.

El punto de impacto y el centro del círculo deben expresarse en el mismo espacio:

- coordenadas locales del mismo contenedor UI; o
- coordenadas de mundo, si ambos son objetos de mundo.

No se deben comparar directamente:

- `screenPosition` con `localPosition`;
- `worldPosition` con `anchoredPosition`;
- posiciones de Canvas distintos sin conversión.

---

## 12. Validación geométrica de la captura

En el momento exacto del impacto:

```text
posicionImpactoFrasco = (xF, yF)
posicionIndicador = (xI, yI)
```

Calcular:

```text
deltaX = xF - xI
deltaY = yF - yI
```

Distancia euclidiana:

```text
distanciaImpacto =
    sqrt(
        deltaX^2
        + deltaY^2
    )
```

En Unity:

```csharp
float distanciaImpacto = Vector2.Distance(
    posicionImpactoFrasco,
    posicionIndicador
);
```

Regla de éxito:

```text
capturaExitosa =
    distanciaImpacto
    <= radioActual + toleranciaImpacto
```

En Unity:

```csharp
bool capturaExitosa =
    distanciaImpacto <= radioActual + toleranciaImpacto;
```

### 12.1. Borde

Un impacto exactamente en el borde es válido:

```text
distanciaImpacto
== radioActual + toleranciaImpacto
```

produce éxito debido al operador `<=`.

### 12.2. Sin aleatoriedad adicional

Después de calcular la distancia:

- No se genera un porcentaje aleatorio.
- No se utiliza `porcentajeCaptura` como probabilidad.
- No se realiza una segunda comprobación.
- El resultado depende únicamente de la geometría y de las condiciones de estado.

### 12.3. Alternativa con radio del frasco

La regla oficial evalúa el punto de impacto del frasco.

No debe sumarse el radio visual del frasco salvo que el equipo cambie explícitamente la regla a intersección entre dos círculos.

---

## 13. Tiempo máximo

Definir:

```text
tiempoMaximoCaptura > 0
```

Mientras el estado sea `Active`:

```text
tiempoRestante =
    tiempoMaximoCaptura
    - tiempoTranscurrido
```

Si:

```text
tiempoTranscurrido >= tiempoMaximoCaptura
```

y todavía no comenzó la caída:

```text
capturaExitosa = false
motivoFallo = Timeout
```

El temporizador debe detenerse al pasar a `Dropping`, salvo que el diseño indique expresamente que la caída también consume el tiempo restante.

---

## 14. Estados del sistema de captura

Estados mínimos:

```text
Inactive
CheckingInventory
Preparing
Active
Dropping
Resolving
Success
Failure
Closing
```

Transiciones principales:

```text
Inactive
    -> CheckingInventory

CheckingInventory
    -> Failure(NoJar)
    -> Preparing

Preparing
    -> Active

Active
    -> Dropping
    -> Failure(Timeout)
    -> Failure(IndicatorTooSmall)
    -> Failure(Cancelled)

Dropping
    -> Resolving

Resolving
    -> Success
    -> Failure(MissedIndicator)

Success
    -> Closing

Failure
    -> Closing

Closing
    -> Inactive
```

Reglas:

- Un intento no puede iniciarse dos veces.
- Solo `Active` acepta el clic inicial.
- `Dropping` ignora clics.
- `Resolving` no puede ejecutarse dos veces.
- `Success` y `Failure` emiten un resultado una sola vez.
- `Closing` detiene corrutinas y desuscribe eventos.

---

## 15. Motivos de fallo

Enum o estructura equivalente:

```text
None
NoJar
MissedIndicator
Timeout
IndicatorTooSmall
InvalidTarget
Cancelled
AlreadyResolved
InvalidConfiguration
```

Resultado mínimo:

```text
CaptureResult:
    success
    failureReason
    capturedUVGmon
    jarConsumed
    impactDistance
    indicatorRadiusAtImpact
```

El resultado debe emitirse exactamente una vez.

---

## 16. Ejemplo: porcentaje de captura alto

Parámetros:

```text
porcentajeCaptura = 80
radioInicialMinimo = 40
radioInicialMaximo = 100
velocidadReduccionMinima = 10
velocidadReduccionMaxima = 50
```

Normalización:

```text
pCaptura = 80 / 100
pCaptura = 0.8
```

Radio:

```text
radioInicial =
    40 + 0.8 * (100 - 40)

radioInicial =
    40 + 0.8 * 60

radioInicial =
    40 + 48

radioInicial = 88
```

Velocidad:

```text
velocidadReduccion =
    50 - 0.8 * (50 - 10)

velocidadReduccion =
    50 - 0.8 * 40

velocidadReduccion =
    50 - 32

velocidadReduccion = 18
```

Interpretación:

```text
círculo inicial grande
reducción lenta
captura relativamente fácil
```

---

## 17. Ejemplo: porcentaje de captura bajo

Parámetros:

```text
porcentajeCaptura = 20
radioInicialMinimo = 40
radioInicialMaximo = 100
velocidadReduccionMinima = 10
velocidadReduccionMaxima = 50
```

Normalización:

```text
pCaptura = 20 / 100
pCaptura = 0.2
```

Radio:

```text
radioInicial =
    40 + 0.2 * (100 - 40)

radioInicial =
    40 + 0.2 * 60

radioInicial =
    40 + 12

radioInicial = 52
```

Velocidad:

```text
velocidadReduccion =
    50 - 0.2 * (50 - 10)

velocidadReduccion =
    50 - 0.2 * 40

velocidadReduccion =
    50 - 8

velocidadReduccion = 42
```

Interpretación:

```text
círculo inicial pequeño
reducción rápida
captura difícil
```

---
