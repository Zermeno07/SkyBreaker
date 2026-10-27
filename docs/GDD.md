# Sky Breaker

## Portada

Título del proyecto: Sky Breaker  
Documento: Documento de Diseño del Juego (GDD)  
Materia: Creación de videojuegos  
Actividad: Parcial 1, Actividad 1, fase de Preproducción  
Grupo: IDGS10D11  
Integrante(s): [Nombre completo]  
Docente: [Nombre del docente]  
Institución: [Nombre de la institución]  
Fecha de entrega: 11 de septiembre de 2026

## 1. Concepto

Sky Breaker es un videojuego 2D de acción y habilidad para móvil y PC, inspirado en la mecánica clásica de Flappy Bird. El jugador controla a un pequeño pájaro que vuela de forma continua por un cielo lleno de tuberías y debe atravesarlas sin chocar. A diferencia del juego original, aquí el jugador recolecta poderes que cambian la forma de jugar: vidas extras, armas para destruir tuberías, un modo para hacerse pequeño y tuberías especiales que se pueden traspasar. También aparecen enemigos voladores que hay que esquivar o eliminar.

El objetivo del juego es superar el propio récord de puntuación. No existen niveles ni un final. Cada partida es una nueva oportunidad para llegar más lejos que la anterior, lo que da a la experiencia un carácter arcade, rápido y adictivo.

Los objetivos del diseño son tres. Primero, ofrecer partidas cortas, de entre uno y cinco minutos, fáciles de entender y difíciles de dominar. Segundo, mantener la simplicidad de un solo botón para el vuelo y sumar variedad con los poderes y los enemigos. Tercero, motivar la rejugabilidad mediante la superación del récord personal, que se guarda de forma local.

## 2. Planificación

### 2.1 Descripción del concepto

Género: arcade, endless runner de desplazamiento lateral.  
Plataformas objetivo: Android y PC (Windows).  
Público objetivo: jugadores casuales de 10 años en adelante, con partidas rápidas.  
Modo de juego: un jugador, sin conexión.

La historia es sencilla y sirve de contexto. Pico es un pájaro joven que vive en el Cielo de las Tuberías, una región donde una antigua fábrica flotante dejó miles de tuberías suspendidas en el aire. Una plaga de máquinas voladoras defectuosas patrulla la zona. Pico quiere demostrar que es el mejor volador del cielo, así que se lanza a cruzar el laberinto sin detenerse, ayudado por energías misteriosas que flotan entre las tuberías y le dan habilidades especiales.

La temática es una aventura ligera con humor, colores brillantes y un tono amigable. El objetivo principal del jugador es sobrevivir el mayor tiempo posible, sumar puntos al cruzar tuberías y eliminar enemigos, y romper su propio récord cada vez que juegue.

### 2.2 Mecánicas del juego

El control es de una sola acción principal. Al tocar la pantalla en el móvil, o presionar la barra espaciadora o el clic izquierdo en PC, Pico da un aleteo que lo impulsa hacia arriba. Cuando no se toca nada, la gravedad lo jala hacia abajo. El personaje avanza automáticamente hacia la derecha, en realidad es el escenario el que se desplaza hacia la izquierda a velocidad constante que aumenta con el tiempo.

El jugador pierde una vida cuando Pico choca contra una tubería, contra el suelo, contra el techo o contra un enemigo. Si no le quedan vidas, la partida termina. Pico comienza cada partida con una vida. Después de recibir un golpe con vidas de sobra, tiene un breve periodo de invulnerabilidad de dos segundos, en el que parpadea, para evitar perder varias vidas seguidas por un mismo error.

La puntuación funciona así. Cada par de tuberías atravesado suma un punto. Destruir un enemigo suma dos puntos y romper una tubería con el arma suma un punto. El juego guarda la puntuación más alta en el dispositivo y la muestra durante la partida y en la pantalla final.

Los poderes aparecen como orbes flotantes en los espacios entre tuberías, aproximadamente uno cada ocho a doce pares de tuberías, y se activan al tocarlos. La vida extra, con forma de corazón, suma una vida hasta un máximo de tres. El arma, con forma de cañón, otorga cinco disparos. El jugador dispara con un botón secundario y cada proyectil destruye una tubería o un enemigo. El poder de encogerse, con forma de hongo, reduce el tamaño de Pico a la mitad durante ocho segundos, lo que reduce su área de colisión y le permite pasar por espacios más estrechos. Por último, el fantasma, con forma de orbe azul, hace que durante cinco segundos Pico pueda atravesar las tuberías sin chocar. Mientras esté activo, las tuberías se ven translúcidas. Solo puede haber un poder temporal activo a la vez, y al tomar uno nuevo se reemplaza el anterior.

Además de los poderes, existen tuberías especiales dentro del mapa. Las tuberías agrietadas se pueden romper con un disparo. Las tuberías fantasma, de color azul translúcido, se pueden traspasar sin necesidad de ningún poder, y el jugador debe aprender a distinguirlas de las normales. Las tuberías normales no se pueden destruir ni traspasar sin un poder.

Los enemigos aparecen desde la derecha de la pantalla y se mueven en trayectorias distintas. El murciélago mecánico vuela en línea recta a mayor velocidad que el escenario. La avispa robot se mueve en ondas de arriba hacia abajo. El dron vigilante se queda estático en el espacio entre dos tuberías y bloquea parte del paso. Cada enemigo se puede eliminar con un disparo o simplemente esquivar. Los enemigos no persiguen al jugador y no reaparecen.

La dificultad aumenta de forma progresiva. La velocidad del escenario sube poco a poco, el espacio entre tuberías se reduce hasta un mínimo que sigue siendo posible de cruzar, y la frecuencia de enemigos y de tuberías especiales crece conforme aumenta la puntuación.

### 2.3 Arte y diseño visual

El estilo es pixel art 2D con una paleta de colores brillantes y saturados, contornos definidos y animaciones sencillas. Se busca una estética retro con toques modernos, agradable a la vista y con buena legibilidad en pantallas pequeñas. Los sprites de personajes tendrán una resolución base de 32 por 32 píxeles y las tuberías de 64 píxeles de ancho, escalados de forma entera para mantener los píxeles nítidos.

El personaje principal, Pico, es un pájaro pequeño de plumaje azul con pecho claro, ojos grandes y expresivos y un pico amarillo. Tendrá animaciones de vuelo, aleteo, golpe, invulnerabilidad y disparo. Cambia visualmente al tomar poderes, por ejemplo se le ve un cañón pequeño en el ala con el arma, luce más pequeño con el poder de encoger y semitransparente en modo fantasma.

Las tuberías normales son verdes con sombras y brillos simples. Las tuberías agrietadas son de color óxido con grietas visibles. Las tuberías fantasma son azules y translúcidas. Esta diferencia de color y transparencia es fundamental para que el jugador las identifique en fracciones de segundo.

Los enemigos comparten un diseño de máquinas defectuosas, con tonos grises y rojos que contrastan con el fondo. Son el murciélago mecánico, la avispa robot y el dron vigilante, cada uno con una silueta muy distinta para reconocerlos rápidamente.

Los escenarios son un solo mundo con fondo en parallax de tres capas, formado por nubes lejanas, montañas o ruinas de la fábrica flotante y nubes cercanas. Con el aumento de la puntuación, el cielo cambia gradualmente de amanecer a día, atardecer y noche, y después vuelve a empezar, lo que da variedad visual sin necesidad de niveles. El suelo es una franja de nubes oscuras y el techo un borde de nubes densas.

### 2.4 Sonido y música

La música será de estilo chiptune de 8 bits, con un tema alegre y enérgico a unos 120 pulsos por minuto para el juego, y un tema más tranquilo y corto para el menú principal. La pista del juego será un bucle continuo que aumenta ligeramente su velocidad o suma una capa de percusión cuando la puntuación sube, para acompañar la tensión creciente. Al perder se reproduce un jingle breve y triste, y al lograr un nuevo récord uno breve y festivo.

Los efectos de sonido serán sintetizados, cortos y claros. Debe haber un sonido para el aleteo, para cruzar una tubería, para tomar cada tipo de poder con un tono distinto según el poder, para el disparo, para la destrucción de una tubería o de un enemigo, para el golpe recibido, para la pérdida de vida final y para la aparición de los enemigos, con una advertencia sonora breve cuando uno entra en pantalla.

Como directrices generales, los sonidos que se repiten mucho, como el aleteo, deben ser suaves para no cansar al jugador. Los sonidos importantes, como tomar un poder o recibir un golpe, deben destacar. El menú de ajustes permitirá controlar por separado el volumen de la música y de los efectos, y silenciar cada uno.

### 2.5 Interfaz de usuario (UI)

El menú principal tiene el logo del juego, un botón grande de Jugar, un botón de Récord, uno de Ajustes y uno de Créditos, sobre el fondo animado del cielo con Pico volando. Los botones son grandes y con buen espacio entre sí para uso táctil.

El HUD durante la partida se mantiene ligero para no tapar la acción. En la parte superior central se muestra la puntuación actual en números grandes. Debajo, en tamaño pequeño, se muestra el récord. En la esquina superior izquierda se muestran las vidas con iconos de corazón. En la esquina superior derecha hay un botón de pausa. Cuando hay un poder temporal activo, aparece un icono con una barra que se va vaciando junto a las vidas. Cuando se tiene el arma, un contador muestra los disparos restantes. En móvil, en la esquina inferior derecha hay un botón de disparo que solo aparece cuando el arma está activa.

El menú de pausa permite continuar, reiniciar, abrir ajustes o volver al menú principal. La pantalla de fin de partida muestra la puntuación obtenida, el récord y un mensaje destacado si se logró uno nuevo, además de los botones Reintentar y Menú principal. El reintento es inmediato para mantener el ritmo de juego.

La pantalla de ajustes incluye el volumen de música, el volumen de efectos, la opción de vibración en móvil y la opción para reiniciar el récord con confirmación. La interfaz usa una tipografía pixelada legible, colores con buen contraste y elementos que respetan los márgenes seguros de pantallas con muescas.

### 2.6 Niveles o misiones

Sky Breaker no tiene niveles ni misiones. Es un juego de un solo escenario infinito cuyo único objetivo es superar el propio récord. Aun así, la experiencia está estructurada en fases de dificultad que se activan de forma automática según la puntuación, sin interrumpir la partida.

De 0 a 19 puntos se tiene la fase de aprendizaje, con tuberías normales, un espacio amplio entre ellas, velocidad baja y sin enemigos. Aparecen los primeros poderes para que el jugador los conozca. De 20 a 49 puntos empiezan a aparecer las tuberías agrietadas y fantasma y los primeros murciélagos mecánicos, y la velocidad aumenta un poco. De 50 a 99 puntos se suman las avispas robot y los drones vigilantes, los espacios se reducen y los poderes se vuelven más importantes para sobrevivir. A partir de 100 puntos comienza la fase de maestría, donde la velocidad y la densidad de enemigos alcanzan su máximo y se mantienen constantes, de modo que el reto depende de la habilidad y de la buena gestión de poderes.

Los desafíos principales son mantener el ritmo de aleteo en espacios estrechos, decidir cuándo conviene esquivar y cuándo disparar, distinguir las tuberías especiales de las normales y aprovechar bien los poderes temporales.

### 2.7 Aspectos técnicos

Se propone usar el motor Unity, en su versión LTS, con el módulo 2D y el lenguaje C#. Unity permite exportar a Android y a PC con el mismo proyecto, cuenta con buen soporte para juegos 2D y una amplia documentación. Para el control de versiones se usará Git con GitHub. Para el arte se usará Aseprite, para el audio Audacity y una herramienta de síntesis de efectos como Bfxr o ChipTone, y para organizar tareas Trello. La música se compondrá con un editor de chiptune como BeepBox o se tomará de bancos gratuitos con licencia libre.

El proyecto usará físicas 2D de Unity con colisionadores simples, un sistema de generación procedural de tuberías con reutilización de objetos mediante pooling para cuidar el rendimiento, un administrador de juego que controle los estados del juego (menú, jugando, pausa y fin de partida) y almacenamiento del récord con PlayerPrefs.

Los requisitos mínimos para Android son la versión 8.0, 2 GB de RAM y una pantalla de 5 pulgadas. Para PC son Windows 10, un procesador de doble núcleo, 4 GB de RAM y gráficos integrados. El equipo de desarrollo recomendado es un procesador de clase Intel Core i5 o equivalente, 16 GB de RAM y un dispositivo Android para pruebas reales. El juego está pensado para funcionar a 60 cuadros por segundo con un tamaño de instalación menor a 100 MB.

### 2.8 Planificación del desarrollo

Se plantea un desarrollo de 12 semanas con un equipo de cuatro personas. Un diseñador y programador líder se encarga de las mecánicas, la generación de tuberías y la integración general. Un programador se ocupa de los enemigos, poderes, interfaz y guardado de datos. Un artista 2D crea los sprites, animaciones, fondos e interfaz. Un responsable de sonido y pruebas produce la música y los efectos, además de organizar las pruebas de jugabilidad y registrar errores. Si el proyecto lo desarrolla una sola persona, las tareas se pueden repartir por etapas siguiendo el mismo cronograma.

Las semanas 1 y 2 corresponden a la preproducción, con el GDD, la selección de herramientas, el arte conceptual y la configuración del proyecto y del repositorio. Las semanas 3 a 5 son la construcción del prototipo, con el vuelo del personaje, la generación de tuberías, la puntuación, el récord y un poder y un enemigo básicos, todo con arte provisional. Las semanas 6 a 9 son la producción principal, donde se agregan todos los poderes, los tres enemigos, las tuberías especiales, el arte y el audio finales, el HUD y los menús. La semana 10 es de pruebas y balance, con sesiones de juego con usuarios y ajuste de la dificultad. La semana 11 es de corrección de errores, optimización y compilación para Android y PC. La semana 12 es de publicación, con la preparación de la ficha en la tienda, capturas, un video corto y la entrega final.

### 2.9 Presupuesto

El presupuesto estimado para el proyecto completo, en pesos mexicanos, es el siguiente. Los sueldos del equipo de cuatro personas durante tres meses, con un promedio de 15,000 pesos mensuales por persona, suman 180,000 pesos. Las licencias y cuentas de desarrollador, como Aseprite y la cuenta de Google Play, junto con otros gastos menores de software, suman 1,500 pesos. La compra de recursos de audio y complementos suma 2,000 pesos. Un dispositivo Android de gama baja para pruebas cuesta 4,000 pesos. Los gastos de marketing, con un pequeño presupuesto de promoción en redes sociales, suman 6,000 pesos. Los gastos de internet, electricidad y espacio de trabajo durante los tres meses suman 6,000 pesos.

La suma de estos conceptos es de 199,500 pesos. Se añade una reserva del 10 por ciento para imprevistos, equivalente a 19,950 pesos, por lo que el presupuesto total estimado es de 219,450 pesos mexicanos. Unity Personal, Git, GitHub, Audacity y Trello son gratuitos para este alcance, por lo que no generan costo.

## 3. Prototipo

El prototipo es una versión inicial y jugable del juego cuyo propósito es probar si la mecánica central es divertida antes de invertir en el arte y el contenido finales. Se desarrollará en Unity con arte provisional, usando figuras geométricas simples como un círculo para Pico, rectángulos verdes para las tuberías y cuadrados de colores para los poderes y enemigos.

El prototipo incluirá el vuelo con aleteo y gravedad, la generación infinita de tuberías con espacios aleatorios, la detección de colisiones y el fin de partida, la puntuación con el récord guardado, un poder de cada tipo básico (vida extra y encogerse), un solo enemigo con trayectoria recta y una pantalla mínima de inicio y de reinicio. Las tuberías especiales, el arma, los demás enemigos, el audio final y los menús completos quedan para la etapa de producción.

Para probar la jugabilidad se harán sesiones con entre cinco y diez personas que no conozcan el proyecto. Cada una jugará durante unos cinco minutos y se observará y registrará cuánto dura la partida promedio, en qué punto suelen perder, si entienden los poderes sin explicación y si el aleteo se siente preciso y justo. Al final se les hará una breve encuesta para saber si el juego les resultó divertido, si la dificultad les pareció adecuada y si volverían a jugar para superar su récord.

Con los resultados se ajustarán valores como la fuerza del aleteo, la gravedad, la velocidad del escenario, el tamaño del espacio entre tuberías y la frecuencia de aparición de poderes y enemigos. Si el prototipo confirma que el juego es entretenido, se continúa con la producción principal. Si no, se corrigen las mecánicas antes de avanzar.

El prototipo se encuentra en la carpeta SkyBreakerUnity de este repositorio. Todos los valores de ajuste mencionados están reunidos en el archivo SkyBreakerUnity/Assets/Scripts/GameConfig.cs para modificarlos fácilmente después de cada sesión de pruebas.

## 4. Avance del desarrollo (75 %)

En esta etapa se ampliaron los poderes, los enemigos, los escenarios, el arte y el sonido del prototipo. Los cambios respecto a lo planteado arriba son los siguientes.

Se agregó un quinto poder, el escudo, con forma de escudo azul con una cruz dorada, que absorbe el siguiente golpe. Los poderes se desbloquean de forma gradual: la vida extra y el hongo desde el inicio, el cañón a partir de 8 puntos, el fantasma a partir de 12 y el escudo a partir de 18. El primer cañón está garantizado antes de que aparezcan los enemigos, para que el jugador aprenda a disparar.

Se agregó un cuarto enemigo, el cuervo bombardero, que cruza por la parte alta de la pantalla y suelta una bomba cuando Pico pasa por debajo. Cada enemigo tiene una característica distinta. El murciélago mecánico vuela recto y no ataca. La avispa robot vuela en ondas y lanza aguijones apuntando a Pico. El dron vigilante se queda quieto entre las tuberías, dispara láseres horizontales y resiste dos disparos. El cuervo bombardero ataca desde arriba. Antes de cada ataque el enemigo parpadea en rojo y suena una alerta.

La aparición de enemigos se adelantó y se suavizó. El primer enemigo sale a los 10 puntos y cada 5 puntos se suma uno nuevo: el murciélago a los 10, la avispa a los 15, el dron a los 20 y el cuervo a los 25. Cada enemigo nuevo se anuncia en pantalla. Hasta los 30 puntos solo puede haber un enemigo a la vez y siempre hay al menos dos tuberías de descanso entre uno y otro. La probabilidad de aparición empieza baja y sube poco a poco, y los ataques empiezan lentos y se aceleran gradualmente hasta la fase de maestría.

En lugar de un solo mundo con ciclo de día y noche, el juego tiene tres escenarios: el Cielo de las Tuberías, de día y con islas flotantes; la Fábrica Flotante, al atardecer, con siluetas industriales, engranes, humo y chispas; y la Tormenta Nocturna, con estrellas, luna, lluvia y relámpagos. El escenario cambia cada 40 puntos con una transición y un aviso, cada uno tiene su propia música y sus propios colores de tuberías, y el jugador puede elegir el escenario inicial en el menú.

El arte provisional de figuras geométricas se reemplazó por pixel art: Pico tiene tres cuadros de animación de aleteo, y los enemigos, los poderes y los proyectiles tienen sprites propios. Se agregaron partículas, temblor de cámara y textos flotantes de puntos. El sonido se genera por código con estilo chiptune: hay efectos para cada acción y una canción por escenario, cuya capa de percusión aumenta con la dificultad. La interfaz incluye ajustes de volumen, vibración y borrado del récord, además de una pantalla de créditos.

Después de las primeras pruebas se ajustó la dificultad. Las tuberías estaban demasiado juntas, así que la distancia horizontal entre ellas pasó de 4.2 a 6.5 unidades y el hueco para pasar se amplió. También se facilitó el combate: el cañón da 8 disparos con asistencia de puntería, el dron vigilante cae con un solo disparo, los enemigos y sus proyectiles son más lentos, y sus zonas de choque son un poco más chicas que su dibujo. Hasta los 40 puntos solo puede haber un enemigo en pantalla y después, como máximo, dos.

Se agregó la selección de personaje en el menú principal. Además de Pico, el jugador puede elegir a Wicho L Pop's, Jorge Nitales, Justo Prime o Paleteado, cabezas voladoras con un borde blanco tipo estampa y un sonido de aleteo propio. Ambos personajes se controlan igual y pueden usar todos los poderes.

Cada personaje cabeza tiene un súper poder exclusivo que aparece como un orbe naranja y dura 10 tuberías, tiempo en el que el personaje no recibe daño y los enemigos que toca salen volando. El cigarro de Wicho L Pop's hace que pase a toda velocidad, con humo saliendo de su boca y un efecto de alucinación en el que el fondo se derrite, se estira y cambia de colores. La cerveza de Jorge Nitales lo sube a un carro que avanza por el suelo y rompe las tuberías al chocar. Las papitas de Justo Prime hacen desaparecer las tuberías y en su lugar aparece una laguna. La moto de Paleteado convierte las tuberías en árboles que destroza a su paso.
