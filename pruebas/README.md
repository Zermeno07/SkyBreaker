# Pruebas automatizadas de Sky Breaker

Estas pruebas ejecutan los scripts reales del juego (`SkyBreakerUnity/Assets/Scripts`) sin abrir Unity.
`FakeUnity.cs` simula lo mínimo del motor y `Tests.cs` contiene las 24 pruebas, incluido un
piloto automático que juega partidas completas.

Esta carpeta está fuera de `Assets`, así que Unity no la compila.

## Cómo ejecutarlas (Linux o macOS con Mono)

```
mcs -out:tests.exe pruebas/FakeUnity.cs pruebas/Tests.cs SkyBreakerUnity/Assets/Scripts/*.cs
NGAMES=40 mono tests.exe
```

`NGAMES` es el número de partidas de la prueba de jugabilidad (P24). El último resultado está en `resultados.txt`.
