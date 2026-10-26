# Sky Breaker

Videojuego 2D arcade tipo endless runner inspirado en Flappy Bird, con poderes y enemigos.
Proyecto de la materia Creación de videojuegos (grupo IDGS10D11).

- Documento de diseño (GDD): [docs/GDD.md](docs/GDD.md)
- Proyecto de Unity (prototipo jugable): [SkyBreakerUnity/](SkyBreakerUnity/)

## Estado del prototipo (avance 75 %)

Ya funciona:

- Pico en pixel art con animación de aleteo, inclinación, estiramiento al aletear, parpadeo al recibir daño y aspecto distinto según el poder activo.
- Vuelo con aleteo (toque, clic o ESPACIO) y gravedad; tuberías infinitas con pooling y textura sombreada.
- Tuberías especiales: agrietadas (se rompen con un disparo, desde 25 puntos) y fantasma (se traspasan, desde 30 puntos).
- Vidas (empieza con 1, máximo 3) con invulnerabilidad de 2 segundos, puntuación y récord guardado.
- 5 poderes:
  - Corazón: +1 vida.
  - Cañón: 5 disparos (desde 8 puntos; el primero está garantizado antes de que salgan los enemigos).
  - Hongo: encoge a Pico durante 8 s.
  - Fantasma: atraviesa tuberías durante 5 s (desde 12 puntos).
  - Escudo: absorbe un golpe (desde 18 puntos).
- 4 enemigos con comportamientos y ataques distintos, que aparecen poco a poco:
  - 10 puntos, Murciélago mecánico: vuela recto, sin ataque.
  - 15 puntos, Avispa robot: vuela en ondas y lanza aguijones apuntando a Pico.
  - 20 puntos, Dron vigilante: se queda quieto, dispara láseres y resiste 2 disparos.
  - 25 puntos, Cuervo bombardero: cruza por arriba y suelta una bomba.

  Cada enemigo nuevo se anuncia en pantalla. Al principio solo puede haber uno a la vez, con descanso entre uno y otro, y sus ataques son lentos y avisan antes (parpadean en rojo y suenan). La frecuencia sube poco a poco con la puntuación.
- 3 escenarios con fondo en parallax y su propia música: Cielo de las Tuberías, Fábrica Flotante (engranes, humo y chispas) y Tormenta Nocturna (estrellas, luna, lluvia y relámpagos). El escenario cambia cada 40 puntos, y el inicial se elige en el menú.
- Sonido chiptune generado por código: más de 20 efectos y una canción por escenario más la del menú. La percusión aumenta con la dificultad.
- Partículas (explosiones, plumas, destellos, escombros), temblor de cámara y textos flotantes de puntos.
- Menú con selector de escenario, ajustes (volumen de música y efectos, vibración, borrar récord con confirmación), créditos, pausa y fin de partida con estadísticas.

Pendiente (último 25 %): arte final dibujado a mano, más pruebas de balance con jugadores, compilación y pruebas en Android, pantalla de tutorial y pulido general.

Todo el arte y el audio se generan por código (pixel art definido como texto en `PixelArt.cs`), así que no hay que importar imágenes ni sonidos ni configurar nada en la escena.

## Cómo abrirlo en tu computadora (desde cero)

Solo necesitas Unity Hub con un editor de Unity 6 (o 2022.3 LTS) y Git o VS Code.

1. Descarga el repositorio.
   - Con VS Code: abre la paleta de comandos (Ctrl+Shift+P), escribe `Git: Clone`, pega la URL
     `https://github.com/Zermeno07/SkyBreaker.git` y elige una carpeta (por ejemplo Documentos).
   - Si no quieres usar Git: en GitHub, botón verde "Code" > "Download ZIP" y descomprímelo.
   - Importante: cambia a la rama donde está el prototipo (o fusiónala a `main` primero).
2. Abre Unity Hub > Proyectos > botón "Agregar" (Add) > "Agregar proyecto desde disco"
   y selecciona la carpeta `SkyBreakerUnity` (la que contiene `Assets`, `Packages` y `ProjectSettings`).
3. Si Unity Hub dice que falta la versión del editor, elige la versión que tengas instalada y acepta.
   La primera apertura tarda unos minutos porque Unity genera sus archivos internos.
4. Cuando abra el editor, presiona el botón Play (▶). El juego se crea solo en cualquier escena.
5. Guarda la escena con Ctrl+S como `Assets/Scenes/Juego.unity` (necesaria para compilar después).

### Si el paso 2 no funciona (plan B)

1. En Unity Hub crea un proyecto nuevo con la plantilla "2D (Built-In Render Pipeline)" o "Universal 2D".
2. Copia la carpeta `SkyBreakerUnity/Assets/Scripts` del repositorio dentro de la carpeta `Assets` de tu proyecto nuevo.
3. Vuelve a Unity, espera a que compile y presiona Play.

## Controles

- Aletear: clic izquierdo, toque en pantalla, ESPACIO, W o flecha arriba.
- Disparar (con el cañón): X, F, flecha derecha, clic derecho o el botón DISPARAR de abajo a la derecha.
- Pausa: ESC, P o el botón "II" de arriba a la derecha.
- En la pantalla de fin de partida: clic o ESPACIO para reintentar, ESC para volver al menú.

## Ajustar la dificultad

Todos los valores (gravedad, fuerza del aleteo, velocidad, tamaño de huecos, frecuencia de poderes
y enemigos, etc.) están en `SkyBreakerUnity/Assets/Scripts/GameConfig.cs`.
Cámbialos después de las pruebas de jugabilidad descritas en la sección 3 del GDD.

## Compilar para PC o Android

File > Build Profiles (o Build Settings) > agrega la escena `Juego` > elige Windows o Android > Build.
Para Android necesitas instalar el módulo "Android Build Support" desde Unity Hub > Installs.

## Estructura de los scripts

- `GameBootstrap.cs`: crea el juego automáticamente al presionar Play.
- `GameManager.cs`: estados, generación de tuberías, poderes y enemigos, colisiones, puntuación y escenarios.
- `GameUI.cs`: menú, HUD, pausa, ajustes, créditos, fin de partida y lectura de controles.
- `GameConfig.cs`: todos los valores de ajuste (dificultad, poderes, enemigos).
- `PlayerBird.cs`: Pico, su animación y sus poderes.
- `PipePair.cs`: par de tuberías (normal, agrietada o fantasma).
- `Enemy.cs`: los 4 enemigos y sus ataques.
- `Projectile.cs`: disparos de Pico, aguijones, láseres y bombas.
- `PowerUp.cs`: los 5 poderes.
- `Scenery.cs`: los 3 escenarios con parallax y efectos.
- `AudioManager.cs`: efectos de sonido y música sintetizados.
- `Fx.cs`: partículas.
- `PixelArt.cs`: sprites de pixel art (Pico, enemigos, poderes, proyectiles).
- `SpriteFactory.cs`: texturas generadas (nubes, montañas, fábrica, engranes, tuberías, etc.).
