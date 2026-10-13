# Sky Breaker

Videojuego 2D arcade tipo endless runner inspirado en Flappy Bird, con poderes y enemigos.
Proyecto de la materia Creación de videojuegos (grupo IDGS10D11).

- Documento de diseño (GDD): [docs/GDD.md](docs/GDD.md)
- Proyecto de Unity (prototipo jugable): [SkyBreakerUnity/](SkyBreakerUnity/)

## Estado del prototipo (avance 50 %)

Ya funciona:

- Vuelo de Pico con aleteo (toque, clic o ESPACIO) y gravedad.
- Generación infinita de tuberías con huecos aleatorios y reutilización de objetos (pooling).
- Colisiones con tuberías, suelo, techo y enemigos.
- Sistema de vidas (empieza con 1, máximo 3) con 2 segundos de invulnerabilidad y parpadeo.
- Puntuación (1 punto por tubería) y récord guardado con PlayerPrefs.
- Poderes: corazón (+1 vida) y hongo (Pico se encoge a la mitad durante 8 segundos).
- Enemigo: murciélago mecánico en línea recta, aparece a partir de 20 puntos.
- Dificultad progresiva: velocidad sube y huecos se reducen hasta los 100 puntos (fase de maestría).
- Fondo en parallax de 3 capas y cielo que cambia de amanecer a día, atardecer y noche.
- Menú principal, HUD (puntuación, récord, vidas, barra del poder, botón de pausa), menú de pausa y pantalla de fin de partida con aviso de nuevo récord.

Pendiente (producción): arma y disparo, poder fantasma, tuberías agrietadas y fantasma,
avispa robot y dron vigilante, sonido y música, arte final en pixel art, pantallas de ajustes y créditos.

Todo el arte es provisional y se genera por código (círculos y rectángulos), así que no hay que
importar imágenes ni configurar nada en la escena.

## Cómo abrirlo en tu computadora (desde cero)

Solo necesitas Unity Hub con un editor de Unity 6 (o 2022.3 LTS) y Git o VS Code.

1. Descarga el repositorio.
   - Con VS Code: abre la paleta de comandos (Ctrl+Shift+P), escribe `Git: Clone`, pega la URL
     `https://github.com/zermeno07/skybreaker.git` y elige una carpeta (por ejemplo Documentos).
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
- `GameManager.cs`: estados (menú, jugando, pausa, fin), generación, colisiones, puntuación, récord e interfaz.
- `PlayerBird.cs`: Pico, su vuelo, invulnerabilidad y el poder de encogerse.
- `PipePair.cs`: par de tuberías reutilizable.
- `PowerUp.cs`: orbes de poder (vida extra y encoger).
- `Enemy.cs`: murciélago mecánico.
- `BackgroundScroller.cs`: fondo en parallax, suelo y techo de nubes.
- `SpriteFactory.cs`: genera las figuras del arte provisional.
- `GameConfig.cs`: valores de ajuste.
