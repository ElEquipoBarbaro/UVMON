# UVGmon — Especificación matemática del sistema de combate

**Ruta recomendada:** `Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md`  
**Alcance:** únicamente combate. El sistema de captura queda fuera de este documento.

---

## 1. Objetivo

Este archivo es la fuente única de verdad para implementar y probar:

- QTE de ataque por circunferencia.
- Velocidad del QTE según `porcentajeAtaque`.
- Comprobación de acertividad.
- Fórmula de daño normal.
- Ataques críticos de derrota instantánea.
- Vida por extremidad.
- Vida global del enemigo.
- Estado y sprite de extremidades dañadas.
- Derrota y prevención de eventos duplicados.

## 2. Datos mínimos por extremidad

Cada parte atacable debe tener, como mínimo:

```text
idParte
nombreParte
vidaMaxima
vidaActual
porcentajeAtaque
porcentajeAcertividad
estadoDanado
referenciaVisualNormal
referenciaVisualDanada
```

Todos los porcentajes deben limitarse al rango `0..100`. La vida no puede quedar por debajo de `0`.

---

# PARTE I — QTE DE ATAQUE Y DAÑO

## 18. Separación entre captura y ataque

El QTE de ataque conserva una lógica distinta:

```text
CAPTURA:
    un círculo indicador se mueve
    el círculo se hace pequeño
    el frasco cae
    se evalúa distancia geométrica

ATAQUE:
    una circunferencia se reduce
    el jugador hace clic
    se evalúa coincidencia con una zona objetivo
    después se comprueba acertividad
    después se calcula daño
```

El porcentaje de captura y el porcentaje de ataque tienen efectos opuestos sobre la velocidad:

```text
porcentajeCaptura alto:
    reducción de captura más lenta

porcentajeAtaque alto:
    QTE de ataque más rápido
```

---

## 19. Normalización del porcentaje de ataque

```text
pAtaque =
    clamp(porcentajeAtaque, 0, 100) / 100
```

En Unity:

```csharp
float pAtaque = Mathf.Clamp01(porcentajeAtaque / 100f);
```

---

## 20. Velocidad del QTE de ataque

Usar:

```text
velocidadAtaqueMinima
velocidadAtaqueMaxima
```

Restricciones:

```text
velocidadAtaqueMaxima >= velocidadAtaqueMinima
velocidadAtaqueMinima > 0
```

Fórmula:

```text
velocidadQTEAtaque =
    velocidadAtaqueMinima
    + pAtaque
    * (
        velocidadAtaqueMaxima
        - velocidadAtaqueMinima
      )
```

En Unity:

```csharp
float velocidadQTEAtaque = Mathf.Lerp(
    velocidadAtaqueMinima,
    velocidadAtaqueMaxima,
    pAtaque
);
```

Interpretación:

```text
porcentajeAtaque alto:
    QTE de ataque más rápido

porcentajeAtaque bajo:
    QTE de ataque más lento
```

---

## 21. Radio del QTE de ataque

Actualización:

```text
radioAtaqueActual =
    max(
        radioAtaqueLimite,
        radioAtaqueActual
        - velocidadQTEAtaque * deltaTime
    )
```

En Unity:

```csharp
radioAtaqueActual = Mathf.Max(
    radioAtaqueLimite,
    radioAtaqueActual - velocidadQTEAtaque * deltaTime
);
```

---

## 22. Evaluación del clic del QTE de ataque

Definir:

```text
radioAtaqueObjetivo
toleranciaQTEAtaque
```

Cuando el jugador hace clic:

```text
diferenciaRadios =
    abs(
        radioAtaqueActual
        - radioAtaqueObjetivo
    )
```

Éxito:

```text
qteExitoso =
    diferenciaRadios
    <= toleranciaQTEAtaque
```

Fallo:

```text
qteExitoso =
    diferenciaRadios
    > toleranciaQTEAtaque
```

Si la circunferencia supera la zona válida sin clic:

```text
qteExitoso = false
```

Un QTE fallido detiene el ataque:

```text
danoFinal = 0
```

No se comprueba acertividad ni se genera variación de daño.

---

## 23. Acertividad

La acertividad se evalúa únicamente si:

```text
qteExitoso == true
```

Limitar:

```text
porcentajeAcertividad =
    clamp(porcentajeAcertividad, 0, 100)
```

Normalización para la fórmula de daño:

```text
pAcertividad =
    porcentajeAcertividad / 100
```

Generar una sola vez por ataque:

```text
valorImpacto = random(0, 100)
```

Se recomienda una distribución `float` uniforme:

```csharp
float valorImpacto = randomProvider.Range(0f, 100f);
```

Regla:

```text
si valorImpacto <= porcentajeAcertividad:
    ataqueImpacta = true

si valorImpacto > porcentajeAcertividad:
    ataqueImpacta = false
    danoFinal = 0
```

El valor debe guardarse. No debe generarse otra vez durante el mismo ataque.

---

## 24. Orden obligatorio del cálculo de ataque

```text
1. Seleccionar extremidad.
2. Obtener porcentajeAtaque.
3. Obtener porcentajeAcertividad.
4. Ejecutar QTE de ataque.
5. Si falla el QTE:
       danoFinal = 0
       terminar.
6. Generar valorImpacto una sola vez.
7. Comprobar acertividad.
8. Si falla:
       danoFinal = 0
       terminar.
9. Comprobar ataque crítico.
10. Si es crítico:
       danoFinal = vidaGlobalActualEnemigo.
11. Si no es crítico:
       generar variacionAleatoria una sola vez.
12. Calcular danoFinal.
13. Aplicar daño a la extremidad.
14. Aplicar daño a la vida global.
15. Actualizar interfaz.
16. Actualizar sprite si la extremidad llegó a 0.
17. Comprobar derrota global.
```

---

## 25. Variación aleatoria del daño

Solo se genera después de superar QTE y acertividad:

```text
variacionAleatoria =
    random(0.9, 1.1)
```

En Unity:

```csharp
float variacionAleatoria =
    randomProvider.Range(0.9f, 1.1f);
```

La variación se genera una sola vez por ataque.

---

## 26. Fórmula de daño normal

```text
danoFinal =
    danoBase
    * (porcentajeAtaque / 100)
    * (porcentajeAcertividad / 100)
    * variacionAleatoria
```

Usando variables normalizadas:

```text
danoFinal =
    danoBase
    * pAtaque
    * pAcertividad
    * variacionAleatoria
```

El daño no puede ser negativo:

```text
danoFinal = max(0, danoFinal)
```

### 26.1. Redondeo

Debe mantenerse el tipo numérico usado por el proyecto.

Si la vida usa enteros:

```text
danoFinalEntero =
    round(danoFinal)
```

En Unity:

```csharp
int danoFinalEntero = Mathf.RoundToInt(danoFinal);
```

Si la vida usa `float`, se conserva el valor decimal.

La política debe ser uniforme en todos los ataques.

---

## 27. Ejemplo completo de daño normal

Datos:

```text
danoBase = 100
porcentajeAtaque = 60
porcentajeAcertividad = 80
variacionAleatoria = 1.05
valorImpacto = 50
qteExitoso = true
```

Acertividad:

```text
50 <= 80
```

Resultado:

```text
ataqueImpacta = true
```

Normalización:

```text
pAtaque = 60 / 100 = 0.6
pAcertividad = 80 / 100 = 0.8
```

Daño:

```text
danoFinal =
    100
    * 0.6
    * 0.8
    * 1.05
```

```text
danoFinal = 50.4
```

Si la vida usa enteros:

```text
danoFinalEntero = 50
```

---

## 28. Ataque crítico de derrota instantánea

Un ataque crítico instantáneo requiere simultáneamente:

```text
qteExitoso == true
ataqueImpacta == true
porcentajeAtaque >= 100
```

Entonces:

```text
danoFinal =
    vidaGlobalActualEnemigo
```

Después:

```text
vidaGlobalActualEnemigo = 0
enemigoDerrotado = true
```

Reglas:

- La acertividad continúa aplicándose.
- Un porcentaje de ataque de 100 no garantiza el impacto.
- Si falla el QTE, no hay crítico.
- Si falla la acertividad, no hay crítico.
- Las partes críticas deben tener acertividad baja o una ventana de QTE exigente para mantener el balance.
- La derrota debe emitirse una sola vez.

---

## 29. Vida de la extremidad

Definir:

```text
vidaExtremidadAnterior
vidaExtremidadActual
```

Aplicación:

```text
vidaExtremidadNueva =
    max(
        0,
        vidaExtremidadActual - danoFinal
    )
```

En Unity:

```csharp
vidaExtremidadActual = Mathf.Max(
    0f,
    vidaExtremidadActual - danoFinal
);
```

No debe quedar por debajo de cero.

---

## 30. Cambio de estado de una extremidad

La extremidad cambia a dañada únicamente al cruzar por primera vez el umbral:

```text
vidaExtremidadAnterior > 0
y
vidaExtremidadNueva <= 0
```

Entonces:

```text
estadoDanado = true
actualizarSprite = true
```

La actualización debe ejecutarse una sola vez.

Posibles cambios visuales:

- Vendajes.
- Curitas.
- Heridas.
- Cambio de postura.
- Variante del sprite.
- Indicador de parte inutilizada.

No se debe generar arte nuevo automáticamente si el proyecto no tiene los recursos.

---

## 31. Vida global

Por defecto:

```text
danoVidaGlobal = danoFinal
```

Si existe un multiplicador configurable:

```text
danoVidaGlobal =
    danoFinal * multiplicadorVidaGlobal
```

Valor predeterminado:

```text
multiplicadorVidaGlobal = 1.0
```

Aplicación:

```text
vidaGlobalNueva =
    max(
        0,
        vidaGlobalActual - danoVidaGlobal
    )
```

En Unity:

```csharp
vidaGlobalActual = Mathf.Max(
    0f,
    vidaGlobalActual - danoVidaGlobal
);
```

El daño se aplica una sola vez a la extremidad y una sola vez a la vida global.

No deben existir suscripciones duplicadas que apliquen el mismo daño dos veces.

---

---

# PARTE II — CONFIGURACIÓN DEL COMBATE

## 32. Parámetros globales recomendados

```text
velocidadAtaqueMinima
velocidadAtaqueMaxima
radioAtaqueInicial
radioAtaqueObjetivo
radioAtaqueLimite
toleranciaQTEAtaque
multiplicadorVidaGlobal
```

Validaciones:

```text
velocidadAtaqueMaxima >= velocidadAtaqueMinima > 0
radioAtaqueInicial > radioAtaqueObjetivo
radioAtaqueObjetivo >= radioAtaqueLimite
radioAtaqueLimite >= 0
toleranciaQTEAtaque >= 0
multiplicadorVidaGlobal >= 0
```

## 33. Aleatoriedad comprobable

La implementación debe permitir controlar la aleatoriedad en pruebas.

Operaciones requeridas:

```text
Range(0, 100)   -> comprobación de acertividad
Range(0.9, 1.1) -> variación del daño
```

Reglas:

- `valorImpacto` se genera una sola vez por ataque.
- `variacionAleatoria` se genera una sola vez por ataque normal.
- No usar una semilla fija en producción.
- Las pruebas deben poder suministrar valores predeterminados.

---

# PARTE III — PRUEBAS MATEMÁTICAS MÍNIMAS

## 34. QTE de ataque

1. `porcentajeAtaque < 0` se limita a `0`.
2. `porcentajeAtaque > 100` se limita a `100`.
3. Porcentaje `0` produce velocidad mínima.
4. Porcentaje `100` produce velocidad máxima.
5. Porcentaje `20` produce menor velocidad que `80`.
6. Clic dentro de la tolerancia: éxito.
7. Clic exactamente en el borde: éxito.
8. Clic fuera de la tolerancia: fallo.
9. No hacer clic antes de superar la zona: fallo.
10. QTE fallido produce daño `0`.

## 35. Acertividad y daño

11. Acertividad fallida produce daño `0`.
12. `valorImpacto` se genera una vez.
13. `variacionAleatoria` se genera una vez.
14. El ejemplo `100 × 0.6 × 0.8 × 1.05` produce `50.4`.
15. Si la vida usa enteros, el ejemplo se redondea de forma consistente.
16. El daño nunca es negativo.
17. La vida de la extremidad no baja de `0`.
18. La vida global no baja de `0`.
19. El multiplicador de vida global funciona.
20. El mismo daño no se aplica dos veces.

## 36. Crítico, estado visual y derrota

21. Ataque `100` con QTE fallido no derrota.
22. Ataque `100` con acertividad fallida no derrota.
23. Ataque `100` con QTE e impacto exitosos usa la vida global actual.
24. La derrota se emite una sola vez.
25. La extremidad se marca dañada al cruzar de vida positiva a `0`.
26. El cambio visual se ejecuta una sola vez.
27. Ataques posteriores no repiten el cambio.
28. No quedan eventos o corrutinas activos.
29. No existen suscripciones duplicadas.
30. La interfaz refleja la vida de la parte y la vida global correctas.

---

# PARTE IV — RESTRICCIONES TÉCNICAS

## 37. Reglas obligatorias

- Mantener el QTE de ataque separado del sistema de captura.
- No generar acertividad si el QTE falló.
- No regenerar `valorImpacto` durante el mismo ataque.
- No regenerar `variacionAleatoria` durante el mismo ataque.
- No aplicar daño dos veces.
- No emitir derrota dos veces.
- No actualizar dos veces el sprite de una misma extremidad dañada.
- No buscar dependencias repetidamente en `Update`.
- No instalar paquetes ni cambiar la versión de Unity.
- No migrar el sistema de entrada.
- No modificar sistemas ajenos al combate.
- Conservar archivos `.meta`, GUID y referencias serializadas.

## 38. Criterios de aceptación

La implementación se considera completa cuando:

1. Puede seleccionarse una extremidad.
2. El porcentaje de ataque modifica la velocidad del QTE.
3. Mayor porcentaje produce un QTE más rápido.
4. El clic se evalúa por diferencia de radios y tolerancia.
5. QTE fallido produce daño `0`.
6. La acertividad se comprueba después del QTE.
7. La fórmula de daño coincide con esta especificación.
8. El ejemplo produce `50.4` o el entero definido por el proyecto.
9. El crítico requiere QTE e impacto exitosos.
10. Cada extremidad mantiene vida independiente.
11. El daño reduce la vida global según el multiplicador.
12. Una extremidad en `0` cambia su estado visual una vez.
13. La derrota se ejecuta una vez.
14. No existen errores de consola relacionados.
15. Las pruebas EditMode y PlayMode están aprobadas.

## 39. Decisiones que Claude Code debe verificar en el proyecto

1. Si la vida usa `int` o `float`.
2. Qué clase representa cada extremidad.
3. Dónde se guardan ataque y acertividad.
4. Qué controlador dibuja el QTE.
5. Qué evento aplica el daño.
6. Qué evento marca la derrota.
7. Qué sprites dañados existen.
8. Si la vida global utiliza multiplicador.
9. Si el QTE usa tiempo escalado o no escalado.
10. Qué sistema de entrada usa el combate.
11. Qué pruebas ya existen.

No deben inventarse decisiones que puedan resolverse inspeccionando el proyecto.
