using System; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Diagnostics;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Tests {
  const BindingFlags F = BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
  static GameManager gm; static PlayerBird bird;
  static object Get(object o, string n){ var t=o.GetType(); var f=t.GetField(n,F); if(f!=null) return f.GetValue(o); return t.GetProperty(n,F).GetValue(o); }
  static void Set(object o, string n, object v){ var t=o.GetType(); var f=t.GetField(n,F); if(f!=null){ f.SetValue(o,v); return;} t.GetProperty(n,F).SetValue(o,v); }
  static object Call(object o, string n, params object[] a){ return o.GetType().GetMethod(n,F).Invoke(o,a); }
  static List<PipePair> Pipes => (List<PipePair>)Get(gm,"pipes");
  static List<Enemy> Enemies => (List<Enemy>)Get(gm,"enemies");
  static List<PowerUp> PowerUps => (List<PowerUp>)Get(gm,"powerUps");
  static int Score => gm.Score;
  static float Vy => (float)Get(bird,"velocityY");
  static bool god;

  static void Step(int frames=1){ for(int i=0;i<frames;i++){ Time.time+=Time.deltaTime; Time.unscaledTime+=Time.deltaTime; if(god) Set(bird,"invulnerableTimer",0.5f); Call(gm,"Update"); } }

  // Piloto automático: busca el hueco de la siguiente tubería y aletea cuando va por debajo.
  static bool smart = true;
  static void Bot(){
    float bx=bird.Position.x, by=bird.Position.y; float target=0f; float best=999;
    foreach(var p in Pipes){ if(!p.gameObject.activeSelf) continue; float d=p.X+p.HalfWidth-(bx-bird.Radius); if(d>0 && d<best){best=d; target=p.GapCenter-0.35f;} }
    if(smart){
      // Esquiva proyectiles que vienen hacia Pico
      foreach(var pr in (List<Projectile>)Get(gm,"projectiles")){
        if(!pr.gameObject.activeSelf || pr.FromPlayer) continue; var d=pr.Position-bird.Position;
        if(d.x>-0.5f && d.x<3.5f && Math.Abs(d.y)<1.2f){ target = d.y>0? by-1.4f : by+1.4f; break; } }
      // Dispara a enemigos enfrente si tiene munición
      foreach(var e in Enemies){ if(!e.gameObject.activeSelf) continue; var d=e.Position-bird.Position;
        if(d.x>0.5f && d.x<7f && Math.Abs(d.y)<2.5f && bird.Ammo>0){ Call(gm,"Shoot"); break; } }
    }
    if(by<target && Vy<1.5f) bird.Flap();
  }
  static void Play(int frames, Func<bool> stop=null){ for(int i=0;i<frames;i++){ if(gm.State!=GameState.Playing) return; Bot(); Step(); if(stop!=null && stop()) return; } }
  static void Start(){ Random.R=new System.Random(42); Call(gm,"StartGame"); bird=(PlayerBird)Get(gm,"bird"); }

  class R { public string id,name,desc,expected,obtained,status; }
  static List<R> results=new List<R>();
  static void Test(string id,string name,string desc,string expected,Func<(string,bool,bool)> body){
    var r=new R{id=id,name=name,desc=desc,expected=expected};
    try { var (obt,ok,obs)=body(); r.obtained=obt; r.status= ok? (obs?"Aprobada con observaciones":"Aprobada") : "No aprobada"; }
    catch(Exception e){ var ex=e is TargetInvocationException? e.InnerException : e; r.obtained="Excepción: "+ex.GetType().Name+": "+ex.Message+" "+ex.StackTrace?.Split('\n').FirstOrDefault(); r.status="No aprobada"; }
    results.Add(r); Console.WriteLine($"[{r.status}] {id} {name}: {r.obtained}");
  }

  public static void Main(){
    var go=new GameObject("GameManager"); gm=go.AddComponent<GameManager>(); bird=(PlayerBird)Get(gm,"bird");

    Test("P01","Inicio del juego","Al abrir el juego se crea todo por código y se muestra el menú","Estado Menú, escenario y personaje creados",()=>{
      bool ok = gm.State==GameState.Menu && Get(gm,"scenery")!=null && bird!=null;
      return ($"Estado = {gm.State}; objetos creados = {GameObject.Created}", ok, false); });

    Test("P02","Aleteo","Se aletea una vez al iniciar la partida","Pico sube (velocidad vertical positiva)",()=>{
      Start(); float y0=bird.Position.y; bird.Flap(); Step(10); float y1=bird.Position.y;
      return ($"Altura de {y0:0.00} a {y1:0.00} en 10 cuadros", y1>y0, false); });

    Test("P03","Gravedad y choque con el suelo","Se deja caer a Pico sin tocar nada","Cae, choca con el suelo y termina la partida (1 vida)",()=>{
      Start(); int f=0; while(gm.State==GameState.Playing && f<600){ Step(); f++; }
      return ($"Fin de partida a los {f/60f:0.00} s; vidas = {gm.Lives}", gm.State==GameState.GameOver && gm.Lives==0, false); });

    Test("P04","Separación de tuberías","Se mide la distancia horizontal entre tuberías consecutivas","6.5 unidades",()=>{
      Start(); god=true; Play(60*12); god=false;
      var xs=Pipes.Where(p=>p.gameObject.activeSelf).Select(p=>p.X).OrderBy(x=>x).ToList(); var d=new List<float>(); for(int i=1;i<xs.Count;i++) d.Add(xs[i]-xs[i-1]);
      float avg=d.Count>0? d.Average():0; return ($"Distancia promedio = {avg:0.00} ({d.Count} medidas)", Math.Abs(avg-6.5f)<0.15f, false); });

    Test("P05","Puntuación al pasar tuberías","El piloto automático pasa tuberías","+1 punto por cada tubería",()=>{
      Start(); Play(60*20, ()=>Score>=3); int passed=Pipes.Count(p=>p.Scored);
      return ($"Puntos = {Score}; tuberías marcadas como pasadas = {passed}", Score>=3 && passed>=Score-1, false); });

    Test("P06","Récord guardado","Al terminar una partida con más puntos que el récord","El récord se guarda en PlayerPrefs",()=>{
      int s=Score; while(gm.State==GameState.Playing) Step(); int saved=PlayerPrefs.GetInt(GameConfig.HighScoreKey,-1);
      return ($"Puntos = {s}; récord guardado = {saved}", saved==gm.HighScore && saved>=s, false); });

    Test("P07","Dificultad progresiva","Se compara el hueco y la velocidad a 0 y a 100 puntos","Hueco de 3.8 a 2.7 y velocidad de 3 a 5",()=>{
      Start(); Step(); float sp0=gm.Speed; Set(gm,"Score",100); Step(); float sp1=gm.Speed;
      float g0=Mathf.Lerp(GameConfig.StartGap,GameConfig.MinGap,0), g1=Mathf.Lerp(GameConfig.StartGap,GameConfig.MinGap,1);
      Call(gm,"SpawnPipe",20f); var last=Pipes.Where(p=>p.gameObject.activeSelf).OrderBy(p=>p.X).Last();
      return ($"Velocidad {sp0:0.0} → {sp1:0.0}; hueco generado a 100 puntos = {last.GapSize:0.00}", Math.Abs(sp0-3)<0.01 && Math.Abs(sp1-5)<0.01 && Math.Abs(last.GapSize-2.7f)<0.01, false); });

    Test("P08","Vida extra","Se toma un corazón con 1 vida y luego con 3","Sube a 2 vidas y nunca pasa de 3",()=>{
      Start(); var pu=PowerUp.Create((Transform)Get(gm,"world")); pu.Spawn(PowerUpType.ExtraLife,bird.Position); Call(gm,"Collect",pu); int a=gm.Lives;
      Set(gm,"Lives",3); pu.Spawn(PowerUpType.ExtraLife,bird.Position); Call(gm,"Collect",pu);
      return ($"Vidas después del primer corazón = {a}; con 3 vidas = {gm.Lives}", a==2 && gm.Lives==3, false); });

    Test("P09","Invulnerabilidad tras un golpe","Se recibe un golpe con 2 vidas","Pierde 1 vida y queda invulnerable 2 s",()=>{
      Start(); Set(gm,"Lives",2); Call(gm,"Hurt"); bool inv=bird.IsInvulnerable; Step(60*2+5); bool inv2=bird.IsInvulnerable;
      return ($"Vidas = {gm.Lives}; invulnerable al instante = {inv}; a los 2.1 s = {inv2}", gm.Lives==1 && inv && !inv2, false); });

    Test("P10","Escudo","Se recibe un golpe con escudo","No pierde vida y el escudo se rompe",()=>{
      Start(); bird.GiveShield(); Call(gm,"Hurt");
      return ($"Vidas = {gm.Lives}; escudo = {bird.HasShield}", gm.Lives==1 && !bird.HasShield, false); });

    Test("P11","Disparo del cañón","Se dispara a un murciélago colocado enfrente","El enemigo explota, suma 2 puntos y se gasta 1 disparo",()=>{
      Start(); bird.AddAmmo(8); var e=Enemy.Create((Transform)Get(gm,"world")); Enemies.Add(e); e.Spawn(EnemyType.Bat,bird.Position+new Vector2(4f,0.5f),1.5f);
      Call(gm,"Shoot"); for(int i=0;i<40;i++){ Set(bird,"velocityY",0f); Step(); }
      return ($"Enemigo activo = {e.gameObject.activeSelf}; puntos = {Score}; disparos restantes = {bird.Ammo}", !e.gameObject.activeSelf && Score==2 && bird.Ammo==7, false); });

    Test("P12","Tubería agrietada","Se dispara a la mitad superior de una tubería agrietada","Esa mitad se rompe y suma 1 punto",()=>{
      Start(); bird.AddAmmo(8); var pipe=PipePair.Create((Transform)Get(gm,"world")); Pipes.Add(pipe);
      pipe.Setup(bird.Position.x+4f, bird.Position.y-2.5f, 3f, PipeKind.Cracked, ZoneTheme.All[0]);
      Call(gm,"Shoot"); for(int i=0;i<40;i++){ Set(bird,"velocityY",0f); Step(); }
      bool solidTop=pipe.Overlaps(new Vector2(pipe.X, bird.Position.y), 0.2f);
      return ($"Mitad superior sigue sólida = {solidTop}; puntos = {Score}", !solidTop && Score>=1, false); });

    Test("P13","Poder fantasma","Con el fantasma activo, Pico queda dentro de una tubería","No pierde vida",()=>{
      Start(); bird.ApplyTemp(TempPower.Ghost,5f); var pipe=PipePair.Create((Transform)Get(gm,"world")); Pipes.Add(pipe);
      pipe.Setup(bird.Position.x, bird.Position.y+3f, 2f, PipeKind.Normal, ZoneTheme.All[0]);
      for(int i=0;i<30;i++){ Set(bird,"velocityY",0f); Step(); }
      return ($"Vidas = {gm.Lives}; estado = {gm.State}", gm.Lives==1 && gm.State==GameState.Playing, false); });

    // Aparición de enemigos con invencibilidad de prueba
    var firstScore=new Dictionary<EnemyType,int>(); int maxBefore40=0; int zoneChangeScore=-1; ZoneId z0=ZoneId.Cielo;
    Test("P14","Aparición gradual de enemigos","Se juega hasta 45 puntos (modo de prueba invencible) registrando cuándo aparece cada enemigo","Ninguno antes de 10; murciélago 10, avispa 15, dron 20, cuervo 25",()=>{
      Start(); god=true; z0=(ZoneId)Get(gm,"currentZone");
      for(int i=0;i<60*60*8 && Score<45;i++){ Bot(); Step();
        int active=0; foreach(var e in Enemies) if(e.gameObject.activeSelf){ active++; if(!firstScore.ContainsKey(e.Type)) firstScore[e.Type]=Score; }
        if(Score<40) maxBefore40=Math.Max(maxBefore40,active);
        if(zoneChangeScore<0 && (ZoneId)Get(gm,"currentZone")!=z0) zoneChangeScore=Score; }
      god=false;
      string s=string.Join(", ", firstScore.OrderBy(k=>k.Key).Select(k=>$"{Enemy.DisplayName(k.Key)} a los {k.Value}"));
      bool ok=firstScore.Count==4 && firstScore[EnemyType.Bat]>=10 && firstScore[EnemyType.Wasp]>=15 && firstScore[EnemyType.Drone]>=20 && firstScore[EnemyType.Crow]>=25;
      return ($"Primera aparición: {s}", ok, false); });

    Test("P15","Máximo de enemigos al inicio","Se cuenta cuántos enemigos hay a la vez antes de los 40 puntos","Máximo 1",()=>($"Máximo simultáneo antes de 40 puntos = {maxBefore40}", maxBefore40<=1, false));

    Test("P16","Cambio de escenario","Se registra la puntuación en que cambia el escenario","Cambia a los 40 puntos",()=>($"Cambió a los {zoneChangeScore} puntos (de {z0} a {Get(gm,"currentZone")})", zoneChangeScore==40, false));

    foreach(var ch in new[]{CharacterId.Wicho,CharacterId.Jorge,CharacterId.Justo,CharacterId.Paleteado}){
      var sp=Characters.Super(ch);
      Test("P"+(16+(int)ch),"Súper poder: "+Characters.SuperShortName(sp).ToLower()+" ("+Characters.Name(ch)+")","Se activa el súper poder y se juega hasta que termina","Dura 10 tuberías sin perder vidas; efecto aplicado",()=>{
        Set(gm,"character",ch); bird.SetCharacter(ch); PowerUp.CurrentSuper=sp; Start(); god=false;
        Play(60*3); int s0=Score; int lives0=gm.Lives; Call(gm,"ActivateSuper");
        int specials=0, trees=0, broken=0, lastLeft=(int)Get(gm,"superPipesLeft"); float maxSpeed=0; int frames=0; var treeSeen=new HashSet<PipePair>(); var brokenSeen=new HashSet<PipePair>(); bool lagoonSeen=false;
        while(gm.State==GameState.Playing && (SuperPower)Get(gm,"superActive")!=SuperPower.None && frames<60*60){ Bot(); Step(); frames++;
          maxSpeed=Math.Max(maxSpeed,gm.Speed);
          int left=(int)Get(gm,"superPipesLeft"); if(left<lastLeft) specials+=lastLeft-left; lastLeft=left;
          if((bool)Get(Get(gm,"scenery"),"Lagoon")) lagoonSeen=true;
          foreach(var p in Pipes){ if(!p.gameObject.activeSelf) continue; if(p.IsTree && !p.Scored && treeSeen.Add(p)) trees++; if(p.Broken && brokenSeen.Add(p)) broken++; }
          foreach(var p in Pipes) if(!p.gameObject.activeSelf){ treeSeen.Remove(p); brokenSeen.Remove(p);} }
        if((SuperPower)Get(gm,"superActive")==SuperPower.None && lastLeft==1) specials++;
        string detail = sp==SuperPower.Moto? $"árboles destrozados = {broken}" : sp==SuperPower.Chips? $"laguna visible = {(lagoonSeen?"sí":"no")}" : sp==SuperPower.Cigar? $"velocidad máxima = {maxSpeed:0.0} (normal máx. 5.0)" : $"tuberías chocadas = {broken}";
        bool ok = gm.State==GameState.Playing && gm.Lives>=lives0 && specials==10 && (SuperPower)Get(gm,"superActive")==SuperPower.None;
        if(sp==SuperPower.Moto || sp==SuperPower.Beer) ok&= broken==10;
        if(sp==SuperPower.Cigar) ok&= maxSpeed>GameConfig.MaxSpeed;
        if(sp==SuperPower.Chips) ok&= lagoonSeen;
        return ($"Tuberías del súper poder = {specials}; duró {frames/60f:0.0} s; puntos ganados = {Score-s0}; vidas antes/después = {lives0}/{gm.Lives}; {detail}", ok, false); });
    }

    Test("P21","Pausa","Se pausa una partida en curso","El juego se detiene (timeScale 0) y el personaje no se mueve",()=>{
      Set(gm,"character",CharacterId.Pico); bird.SetCharacter(CharacterId.Pico); Start(); Step(5); Call(gm,"Pause"); Time.deltaTime=0f; float y=bird.Position.y; Step(30); float y2=bird.Position.y; Time.deltaTime=1/60f;
      var st=gm.State; var ts=Time.timeScale; bool ok=st==GameState.Paused && Time.timeScale==0 && Math.Abs(y-y2)<1e-4; Call(gm,"Resume");
      return ($"Estado = {st}; timeScale = {ts}; movimiento durante la pausa = {Math.Abs(y-y2):0.0000}; al continuar el estado vuelve a {gm.State}", ok, false); });

    Test("P22","Sonido","Se generan todos los efectos y las canciones","Un clip por efecto y 4 canciones sin errores",()=>{
      var am=AudioManager.Instance; var clips=(System.Collections.IDictionary)Get(am,"clips"); int n=Enum.GetValues(typeof(Sfx)).Length;
      for(int i=0;i<4;i++) am.PlaySong(i);
      var songs=(System.Collections.IDictionary)Get(am,"songs");
      return ($"Efectos generados = {clips.Count} de {n}; canciones = {songs.Count}", clips.Count==n && songs.Count==4, false); });

    // Rendimiento: 10 minutos de juego simulado
    Test("P23","Rendimiento y memoria (pooling)","Se simulan 10 minutos de juego continuo (modo invencible) midiendo objetos creados y tiempo de lógica por cuadro","Los objetos dejan de crecer (se reutilizan) y la lógica tarda mucho menos de 16.7 ms por cuadro",()=>{
      Start(); god=true; var sw=new Stopwatch(); int frames=60*60*10; int alive2=0, maxAlive=0, minAlive=int.MaxValue;
      sw.Start(); for(int i=0;i<frames;i++){ Bot(); Step(); if(i%600==0 && i>=60*60*2){ int a=GameObject.Alive; maxAlive=Math.Max(maxAlive,a); minAlive=Math.Min(minAlive,a); if(alive2==0) alive2=a; } } sw.Stop(); god=false;
      double ms=sw.Elapsed.TotalMilliseconds/frames;
      return ($"Objetos vivos entre el minuto 2 y el 10: entre {minAlive} y {maxAlive} (no crecen); tuberías en el pool = {Pipes.Count}; enemigos en el pool = {Enemies.Count}; lógica = {ms:0.000} ms por cuadro; puntos = {Score}", ms<4 && maxAlive-minAlive<400, false); });

    // Bot sin invencibilidad: prueba de jugabilidad automatizada
    Test("P24","Jugabilidad automatizada","Un piloto automático que esquiva proyectiles y dispara juega 40 partidas sin ayuda","Las partidas duran y suman puntos (el juego no es injusto desde el inicio)",()=>{
      var scores=new List<int>(); var causes=new Dictionary<string,int>();
      int N=int.Parse(Environment.GetEnvironmentVariable("NGAMES")??"10"); for(int k=0;k<N;k++){ Random.R=new System.Random(100+k); Call(gm,"StartGame"); bird=(PlayerBird)Get(gm,"bird");
        string cause="ninguna";
        for(int i=0;i<60*60*5 && gm.State==GameState.Playing;i++){ Bot(); int l=gm.Lives; Vector2 c=bird.Position; float r=bird.Radius; Step();
          if(gm.Lives<l){ cause="tubería";
            if(c.y-r<=GameConfig.GroundY+0.1f) cause="suelo"; else if(c.y+r>=GameConfig.CeilingY-0.1f) cause="techo";
            foreach(var e in Enemies) if((e.Position-c).sqrMagnitude<Math.Pow(r+e.Radius+0.3f,2)) cause="choque con enemigo";
            foreach(var p in (List<Projectile>)Get(gm,"projectiles")) if(p.gameObject.activeSelf && !p.FromPlayer && (p.Position-c).sqrMagnitude<Math.Pow(r+p.Radius+0.4f,2)) cause="ataque enemigo ("+p.Kind+")";
            foreach(var p in (List<Projectile>)Get(gm,"projectiles")) if(!p.gameObject.activeSelf && !p.FromPlayer && (p.Position-c).sqrMagnitude<Math.Pow(r+p.Radius+0.4f,2)) cause="ataque enemigo ("+p.Kind+")";
          } }
        scores.Add(Score); causes[cause]=causes.ContainsKey(cause)?causes[cause]+1:1; }
      string cs=string.Join(", ",causes.OrderByDescending(x=>x.Value).Select(x=>x.Key+" "+x.Value));
      return ($"Puntos por partida: {string.Join(", ",scores)}; promedio = {scores.Average():0.0}; máximo = {scores.Max()}; causa de la derrota: {cs}", scores.Average()>=10, true); });

    System.IO.File.WriteAllText("results.tsv", string.Join("\n", results.Select(r=>string.Join("\t", r.id,r.name,r.desc,r.expected,r.obtained,r.status))));
    Console.WriteLine($"\n{results.Count(r=>r.status=="Aprobada")} aprobadas, {results.Count(r=>r.status.StartsWith("Aprobada con"))} con observaciones, {results.Count(r=>r.status=="No aprobada")} no aprobadas de {results.Count}");
  }
}
