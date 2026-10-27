// Simulación mínima pero funcional de la API de Unity para ejecutar la lógica del juego sin el editor.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
namespace UnityEngine {
public class Object {
  public string name; internal bool destroyed;
  public static void Destroy(Object o){ if (o!=null) { var g=o as GameObject; if(g!=null){ g.transform.parent?.children.Remove(g.transform); g.DestroyTree(); } o.destroyed=true; } }
  public static bool operator==(Object a, Object b){ bool an=ReferenceEquals(a,null)||a.destroyed; bool bn=ReferenceEquals(b,null)||b.destroyed; if(an||bn) return an&&bn; return ReferenceEquals(a,b); }
  public static bool operator!=(Object a, Object b)=>!(a==b);
  public override bool Equals(object o)=>ReferenceEquals(this,o); public override int GetHashCode()=>base.GetHashCode();
  public static implicit operator bool(Object o)=>o!=null;
}
public class Component : Object { public GameObject gameObject; public Transform transform=>gameObject.transform;
  public T[] GetComponentsInChildren<T>() where T:Component { var l=new List<T>(); gameObject.Collect(l); return l.ToArray(); }
  public T GetComponent<T>() where T:Component => gameObject.GetComponent<T>(); }
public class Behaviour : Component { public bool enabled=true; }
public class MonoBehaviour : Behaviour { public void Invoke(string m, float t){ var mi=GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); mi?.Invoke(this,null);} }
public class GameObject : Object {
  public static int Created; public static List<GameObject> All=new List<GameObject>(); public static int Alive { get { int n=0; foreach(var g in All) if(!g.destroyed) n++; return n; } }
  readonly List<Component> comps=new List<Component>();
  public Transform transform; public string tag; bool active=true;
  public GameObject(string n){ name=n; Created++; All.Add(this); transform=new Transform(); transform.gameObject=this; comps.Add(transform); }
  public bool activeSelf=>active;
  public void SetActive(bool b){ active=b; }
  public T AddComponent<T>() where T:Component { var c=(T)Activator.CreateInstance(typeof(T),true); c.gameObject=this; comps.Add(c);
    var aw=typeof(T).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); aw?.Invoke(c,null); return c; }
  public T GetComponent<T>() where T:Component { foreach(var c in comps) if(c is T t) return t; return null; }
  internal void Collect<T>(List<T> l) where T:Component { foreach(var c in comps) if(c is T t) l.Add(t); foreach(var ch in transform.children) ch.gameObject.Collect(l); }
  internal void DestroyTree(){ destroyed=true; foreach(var ch in transform.children.ToArray()) ch.gameObject.DestroyTree(); transform.children.Clear(); }
}
public class Transform : Component {
  internal Transform parent; internal List<Transform> children=new List<Transform>();
  public Vector3 localPosition; public Vector3 localScale=new Vector3(1,1,1); public Quaternion localRotation;
  public Quaternion rotation { get=>localRotation; set=>localRotation=value; }
  public Vector3 position { get=> parent==null? localPosition : parent.position+localPosition; set { localPosition = parent==null? value : value-parent.position; } }
  public void SetParent(Transform p, bool w){ parent?.children.Remove(this); parent=p; p?.children.Add(this); }
  public void Rotate(float x,float y,float z){ localRotation.z+=z; }
  public Vector3 TransformPoint(Vector3 v)=>position+v;
}
public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;}
  public static Vector2 zero=>new Vector2(0,0); public static Vector2 one=>new Vector2(1,1);
  public float sqrMagnitude=>x*x+y*y; public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
  public Vector2 normalized { get { float m=magnitude; return m>1e-6f? new Vector2(x/m,y/m):new Vector2(0,0);} }
  public static Vector2 operator-(Vector2 a, Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); public static Vector2 operator+(Vector2 a, Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
  public static Vector2 operator*(Vector2 a, float k)=>new Vector2(a.x*k,a.y*k); public static Vector2 operator*(float k, Vector2 a)=>new Vector2(a.x*k,a.y*k);
  public static implicit operator Vector3(Vector2 v)=>new Vector3(v.x,v.y,0); public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
  public override string ToString()=>$"({x:0.00},{y:0.00})"; }
public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 one=>new Vector3(1,1,1); public static Vector3 zero=>new Vector3(0,0,0);
  public static Vector3 operator+(Vector3 a, Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); public static Vector3 operator-(Vector3 a, Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a, float k)=>new Vector3(a.x*k,a.y*k,a.z*k); }
public struct Quaternion { public float z; public static Quaternion identity=>new Quaternion(); public static Quaternion Euler(float a,float b,float c)=>new Quaternion{z=c}; public static Vector3 operator*(Quaternion q, Vector3 v)=>v; }
public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
  public static Color white=>new Color(1,1,1,1); public static Color black=>new Color(0,0,0,1); public static Color clear=>new Color(0,0,0,0);
  public static Color Lerp(Color a, Color b, float t){t=Mathf.Clamp01(t);return new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}
  public static Color HSVToRGB(float h,float s,float v)=>new Color(v,v,v,1);
  public static Color operator*(Color a, float k)=>new Color(a.r*k,a.g*k,a.b*k,a.a*k); public static Color operator+(Color a, Color b)=>new Color(a.r+b.r,a.g+b.g,a.b+b.b,a.a+b.a);
  public static implicit operator Color32(Color c)=>new Color32((byte)(Mathf.Clamp01(c.r)*255),(byte)(Mathf.Clamp01(c.g)*255),(byte)(Mathf.Clamp01(c.b)*255),(byte)(Mathf.Clamp01(c.a)*255));
  public override string ToString()=>$"{r:0.000},{g:0.000},{b:0.000},{a:0.000}"; }
public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
public struct Bounds { public Vector3 size; }
public static class Mathf { public const float PI=(float)Math.PI, Rad2Deg=57.29578f;
  public static float Clamp(float v,float a,float b)=>v<a?a:v>b?b:v; public static int Clamp(int v,int a,int b)=>v<a?a:v>b?b:v;
  public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b); public static float Min(float a,float b)=>Math.Min(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
  public static float Sin(float a)=>(float)Math.Sin(a); public static float Cos(float a)=>(float)Math.Cos(a); public static float Abs(float a)=>Math.Abs(a);
  public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t); public static float MoveTowards(float c,float t,float d)=> Math.Abs(t-c)<=d? t : c+Math.Sign(t-c)*d;
  public static float Clamp01(float a)=>Clamp(a,0,1); public static int FloorToInt(float a)=>(int)Math.Floor(a); public static int CeilToInt(float a)=>(int)Math.Ceiling(a);
  public static float Floor(float a)=>(float)Math.Floor(a); public static float Round(float a)=>(float)Math.Round(a); public static float Repeat(float t,float l)=>t-(float)Math.Floor(t/l)*l;
  public static float InverseLerp(float a,float b,float v)=> a!=b? Clamp01((v-a)/(b-a)) : 0; public static int RoundToInt(float a)=>(int)Math.Round(a);
  public static float Pow(float a,float b)=>(float)Math.Pow(a,b); public static float Exp(float a)=>(float)Math.Exp(a); public static float Sqrt(float a)=>(float)Math.Sqrt(a);
  public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x); public static bool Approximately(float a,float b)=>Math.Abs(a-b)<1e-5f; }
public static class Random { public static System.Random R=new System.Random(1234);
  public static float Range(float a,float b)=>a+(float)R.NextDouble()*(b-a); public static int Range(int a,int b)=> b<=a? a : R.Next(a,b);
  public static float value=>(float)R.NextDouble(); public static Vector2 insideUnitCircle=>new Vector2((float)R.NextDouble()-0.5f,(float)R.NextDouble()-0.5f); }
public static class Time { public static float deltaTime=1/60f, time, timeScale=1, unscaledTime, unscaledDeltaTime=1/60f; }
public static class Application { public static int targetFrameRate; }
public static class Handheld { public static void Vibrate(){} }
public enum CameraClearFlags { SolidColor }
public class Camera : Behaviour { public static Camera main; public bool orthographic; public float orthographicSize=5, aspect=16f/9f; public CameraClearFlags clearFlags; public Color backgroundColor; public Vector3 WorldToScreenPoint(Vector3 v)=>v; }
public class AudioListener : Behaviour {}
public class AudioClip : Object { public int samples; float[] data; public static AudioClip Create(string n,int len,int ch,int f,bool s)=>new AudioClip{samples=len,data=new float[len],name=n};
  public bool SetData(float[] d,int o){ Array.Copy(d,data,Math.Min(d.Length,data.Length)); return true;} public bool GetData(float[] d,int o){ Array.Copy(data,d,Math.Min(d.Length,data.Length)); return true;} }
public class AudioSource : Behaviour { public static int Plays; public float pitch=1, volume=1; public bool loop, playOnAwake; public AudioClip clip; public void PlayOneShot(AudioClip c, float v){ Plays++; } public void Stop(){} public void PlayScheduled(double t){} }
public static class AudioSettings { public static double dspTime; }
public static class PlayerPrefs { public static Dictionary<string,object> D=new Dictionary<string,object>();
  public static int GetInt(string k,int d)=>D.ContainsKey(k)?(int)D[k]:d; public static float GetFloat(string k,float d)=>D.ContainsKey(k)?(float)D[k]:d;
  public static void SetInt(string k,int v){D[k]=v;} public static void SetFloat(string k,float v){D[k]=v;} public static void Save(){} public static void DeleteKey(string k){D.Remove(k);} }
public enum TextureFormat { RGBA32 } public enum FilterMode { Bilinear, Point } public enum TextureWrapMode { Clamp }
public class Texture : Object {}
public class Texture2D : Texture { public int width,height; public Texture2D(int w,int h,TextureFormat f,bool m){width=w;height=h;} public FilterMode filterMode; public TextureWrapMode wrapMode; public void SetPixels32(Color32[] c){} public void Apply(){} public static Texture2D whiteTexture=new Texture2D(1,1,TextureFormat.RGBA32,false); }
public struct Rect { public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public float x,y,width,height; public float xMax=>x+width; public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<=xMax&&p.y>=y&&p.y<=yMax; public Vector2 center=>new Vector2(x+width/2,y+height/2); }
public class Sprite : Object { public Texture2D texture; public Bounds bounds; public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu)=>new Sprite{texture=t,bounds=new Bounds{size=new Vector3(r.width/ppu,r.height/ppu,0)}}; }
public class Renderer : Component { public bool enabled=true; public int sortingOrder; }
public class SpriteRenderer : Renderer { public Sprite sprite; public Color color=Color.white; }
public static class Screen { public static int width=1920, height=1080; public static Rect safeArea=new Rect(0,0,1920,1080); }
public struct Matrix4x4 {} public enum ScaleMode { ScaleToFit }
public enum TextAnchor { MiddleCenter, MiddleLeft } public enum FontStyle { Bold, Normal }
public class RectOffset { public RectOffset(int a,int b,int c,int d){} }
public class GUIStyleState { public Color textColor; public Texture2D background; }
public class GUIStyle { public GUIStyle(){} public GUIStyle(GUIStyle o){} public TextAnchor alignment; public FontStyle fontStyle; public int fontSize; public bool wordWrap; public RectOffset border; public GUIStyleState normal=new GUIStyleState(), hover=new GUIStyleState(), active=new GUIStyleState(), focused=new GUIStyleState(); public void Draw(Rect r,bool a,bool b,bool c,bool d){} }
public class GUIContent { public static GUIContent none=new GUIContent(); }
public class GUISkin { public GUIStyle label=new GUIStyle(), button=new GUIStyle(); }
public static class GUI { public static GUISkin skin=new GUISkin(); public static Color color; public static Matrix4x4 matrix; public static void Label(Rect r,string t,GUIStyle s){} public static bool Button(Rect r,string t,GUIStyle s)=>false; public static bool Button(Rect r,GUIContent t,GUIStyle s)=>false; public static void DrawTexture(Rect r, Texture t){} public static void DrawTexture(Rect r, Texture t, ScaleMode m){} }
public static class GUIUtility { public static int hotControl; public static void RotateAroundPivot(float a, Vector2 p){} public static void ScaleAroundPivot(Vector2 s, Vector2 p){} }
public enum EventType { MouseDown, KeyDown, KeyUp, Repaint, Layout } public enum KeyCode { None, Space, UpArrow, W, Escape, P, X, F, RightArrow, LeftShift }
public class Event { public static Event current=new Event{type=EventType.Layout}; public EventType type; public KeyCode keyCode; public int button; public Vector2 mousePosition; public void Use(){} }
public enum RuntimeInitializeLoadType { AfterSceneLoad }
public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
}
