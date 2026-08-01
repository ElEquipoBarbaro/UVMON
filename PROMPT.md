
Primero antes que nada lee

CLAUDE.md para entender el contexto del proyecto.

Lee .\COMBAT_SYSTEM_SPEC.md

Y tambien entiende que si necesitas saber algo de cada sistema

lee Assets/Scripts/Systema/*.md  

donde cada sistema tiene un md que explica el mismo


Luego sigue estas reglas


- EL QTE ya esta implementado lee Assets/Scripts/Combat/QTE 
- Todos los cambios deben ser sobre la escena jardinconocimiento
- En donde sucede la batalla es en BattleUI
- Hay una parte de la sigueinte implementacion que debes de crear las partes de un enemigo. Los sprites ya estan posicionados solo para que tu los coloques encima uno del otro de manera estrategica o los reemplaces. Estos son estan en Assets\Enemys\Spider\PARTES

y las siguientes partes son:

- head.png Que es su cabeza (recuerdalo porque mas adelante te pido que si se le golpea es daño critido)
- body.png Es su cuerpo

- body_damaged.png Es el cuerpo con daño recibido






# Fase 5 — Acertividad y daño

## Prompt 12 — Comprobación de acertividad

```text
Implementa o corrige la acertividad según la sección 23 de:

Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md

Reglas:

- evaluar solo si qteExitoso;
- limitar a 0..100;
- generar valorImpacto una vez;
- guardar el valor;
- impacto cuando valorImpacto <= porcentajeAcertividad;
- fallo produce danoFinal = 0;
- permitir aleatoriedad controlada en pruebas.

Agrega pruebas para acertividad 0, 20, 80 y 100.

Compila y detente.
```

## Prompt 13 — Calculador de daño

```text
Implementa o corrige DamageCalculator o equivalente usando las secciones 24 a 27 de:

Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md

Orden:

1. QTE.
2. Acertividad.
3. Crítico.
4. Variación.
5. Daño.

Fórmula:

danoFinal =
    danoBase
    * (porcentajeAtaque / 100)
    * (porcentajeAcertividad / 100)
    * variacionAleatoria

Reglas:

- variación entre 0.9 y 1.1;
- generar una vez;
- daño no negativo;
- respetar int o float del proyecto;
- el calculador devuelve el resultado, pero no aplica el daño dos veces.

Valida el ejemplo:

100 * 0.6 * 0.8 * 1.05 = 50.4

Compila, ejecuta pruebas y detente.
```

## Prompt 14 — Ataque crítico

```text
Implementa el crítico según la sección 28 de:

Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md

Condiciones simultáneas:

- qteExitoso;
- ataqueImpacta;
- porcentajeAtaque >= 100.

Resultado:

danoFinal = vidaGlobalActualEnemigo

Reglas:

- la acertividad sigue aplicándose;
- QTE fallido no derrota;
- acertividad fallida no derrota;
- usar vida global actual;
- derrota una vez;
- daño una vez.

Agrega pruebas de éxito y de ambos tipos de fallo.

Compila y detente.
```

---

# Fase 6 — Extremidades, vida global y visuales

## Prompt 15 — Vida por extremidad

```text
Integra el daño con la extremidad usando las secciones 29 y 30 de:

Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md

Aplicación:

vidaAnterior = vidaActual
vidaActual = max(0, vidaActual - danoFinal)

Reglas:

- aplicar una vez;
- no bajar de 0;
- actualizar barra;
- detectar cruce de >0 a 0;
- marcar estadoDanado una vez;
- emitir el evento una vez.

No cambies todavía los recursos visuales.

Compila y detente.
```

## Prompt 16 — Vida global y derrota

```text
Integra el daño con la vida global según la sección 31 de:

Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md

danoVidaGlobal =
    danoFinal * multiplicadorVidaGlobal

vidaGlobalActual =
    max(0, vidaGlobalActual - danoVidaGlobal)

Reglas:

- multiplicador predeterminado 1.0;
- aplicar una vez;
- vida no negativa;
- derrota una vez;
- recompensas una vez;
- no destruir datos antes de que terminen los sistemas dependientes;
- no modificar captura.

Compila y detente.
```

## Prompt 17 — Cambio visual de la parte

```text
Implementa el cambio visual cuando una extremidad cruza de vida positiva a 0.

Debe:

- afectar solo la parte correspondiente;
- usar referencias existentes;
- mostrar sprite dañado, vendaje, curita o variante disponible;
- ejecutarse una vez;
- no repetirse en ataques posteriores;
- actualizar UI;
- manejar referencias nulas;
- no generar arte nuevo.



## Prompt 18 — Efectos visuales y UI/UX

Como tenemos que mostrarle al usuario que parte esta seleccionando, sobre los botones de acciones indica que parte esta seleccionando, se seleccionan las partes con el mouse y ademas al seleccionar una parte brilla de manera intermitente con un shadow blanco . Esto podes hacerlo cambiando el alpha de la parte.

 Y obvio usa el pointer al momento de seleccionar una parte del cuerpo.




