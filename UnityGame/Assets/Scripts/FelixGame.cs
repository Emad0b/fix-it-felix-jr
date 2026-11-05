using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Felix2D
{
    public sealed class FelixGame : MonoBehaviour
    {
        public GameState State { get; private set; }
        readonly List<Brick> bricks=new List<Brick>();
        readonly SpriteRenderer[,] panes=new SpriteRenderer[3,4];
        SpriteRenderer felix,ralph,hammer;
        Sprite square; AudioSource audioSource;
        bool started,paused,muted; int ralphColumn=1;
        float attackClock=1.5f,moveClock,repairFlash; string capturePath;
        GUIStyle title,body,small,score,button;
        static readonly Color Mint=new Color(.43f,.94f,.76f), Navy=new Color(.035f,.065f,.12f);
        sealed class Brick { public SpriteRenderer Sprite; }
        float X(int c) { return (c-1)*2.1f; }
        float Y(int f) { return -3.25f+f*1.5f; }
        void Awake() {
            State=new GameState();
            var camera=Camera.main;
            if(camera==null){var obj=new GameObject("Camera");obj.tag="MainCamera";camera=obj.AddComponent<Camera>();}
            camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=6.4f;camera.backgroundColor=Navy;
            var tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();tex.filterMode=FilterMode.Point;
            square=Sprite.Create(tex,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            audioSource=gameObject.AddComponent<AudioSource>();
            BuildStage();
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-capturePath")capturePath=args[i+1];
            if(capturePath!=null){started=true;StartCoroutine(Capture());}
        }
        SpriteRenderer Box(string name,float x,float y,float w,float h,Color color,int order=0) {
            var o=new GameObject(name);o.transform.SetParent(transform);o.transform.position=new Vector3(x,y,0);o.transform.localScale=new Vector3(w,h,1);
            var r=o.AddComponent<SpriteRenderer>();r.sprite=square;r.color=color;r.sortingOrder=order;return r;
        }
        Sprite PixelSprite(string[] rows, Dictionary<char,Color> palette) {
            int w=rows[0].Length,h=rows.Length;var t=new Texture2D(w,h);t.filterMode=FilterMode.Point;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)t.SetPixel(x,h-1-y,palette.ContainsKey(rows[y][x])?palette[rows[y][x]]:Color.clear);
            t.Apply();return Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),h/1.0f);
        }
        SpriteRenderer Person(string name,Sprite sprite,float x,float y) {
            var o=new GameObject(name);o.transform.SetParent(transform);o.transform.position=new Vector3(x,y,0);var r=o.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=8;return r;
        }
        void BuildStage() {
            for(int i=0;i<18;i++){float x=-7.5f+i*.9f;Box("Skyline",x,-3.2f,.7f,2+(i%4)*.7f,new Color(.065f,.12f,.20f),-3);}
            Box("Building",0,-.25f,7.3f,7.5f,new Color(.21f,.24f,.33f));
            for(int r=0;r<23;r++)for(int c=0;c<9;c++)Box("Masonry",-3.5f+c*.86f+(r%2)*.3f,-3.8f+r*.32f,.75f,.025f,new Color(.29f,.31f,.40f),1);
            Box("Roof",0,3.65f,7.8f,.25f,new Color(.41f,.45f,.57f),2);
            Box("Ground",0,-4.2f,19,.4f,new Color(.11f,.18f,.23f),2);
            for(int c=0;c<3;c++)for(int f=0;f<4;f++) {
                Box("Window frame",X(c),Y(f)+.22f,1.5f,1.12f,new Color(.63f,.67f,.74f),2);
                panes[c,f]=Box("Window glass",X(c),Y(f)+.22f,1.28f,.91f,new Color(.08f,.13f,.2f),3);
                Box("Window crossbar",X(c),Y(f)+.22f,.055f,1.0f,new Color(.43f,.5f,.6f),4);
                Box("Ledge",X(c),Y(f)-.4f,1.72f,.14f,new Color(.47f,.52f,.62f),5);
            }
            var palette=new Dictionary<char,Color>{{'b',new Color(.23f,.65f,1)}, {'s',new Color(1,.74f,.49f)}, {'d',new Color(.12f,.24f,.4f)}, {'h',new Color(.45f,.22f,.14f)}, {'w',Color.white}};
            felix=Person("Felix",PixelSprite(new[]{"..bbbb..",".bbbbbb.","..ssss..","..sdsw..","..ssss..",".bbbbbb.",".sbbbb s".Replace(" ","."),"..dddd..","..d..d..",".dd..dd."},palette),X(1),Y(0));
            var rpalette=new Dictionary<char,Color>{{'r',new Color(.88f,.31f,.23f)},{'s',new Color(.98f,.66f,.40f)},{'h',new Color(.35f,.17f,.10f)},{'d',new Color(.23f,.13f,.17f)}};
            ralph=Person("Ralph",PixelSprite(new[]{"..hhhhhh..",".hhhhhhhh.",".hssssssh.","..sdssds..","..ssssss..",".rrrrrrrr.","ssrrrrrrss","ssrrrrrrss","..dddddd..","..dd..dd..",".ddd..ddd."},rpalette),X(1),4.25f);ralph.transform.localScale=Vector3.one*1.35f;
            hammer=Box("Hammer",X(1)+.5f,Y(0)+.35f,.35f,.2f,new Color(1,.83f,.35f),9);hammer.enabled=false;
        }
        void Update() {
            if(Input.GetKeyDown(KeyCode.M)){muted=!muted;audioSource.mute=muted;}
            if(Input.GetKeyDown(KeyCode.Return) && (!started || State.Over)){Restart();}
            if(Input.GetKeyDown(KeyCode.R))Restart();
            if(Input.GetKeyDown(KeyCode.Escape) && started && !State.Over)paused=!paused;
            if(!started || paused || State.Over){RefreshVisuals();return;}
            float dt=Time.deltaTime; State.Tick(dt); moveClock-=dt;repairFlash=Mathf.Max(0,repairFlash-dt);
            int dx=0,dy=0;
            if(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))dx=-1;
            else if(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))dx=1;
            if(Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W))dy=1;
            else if(Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S))dy=-1;
            if(moveClock<=0 && (dx!=0||dy!=0)){State.Move(dx,dy);moveClock=.15f;}
            if(Input.GetKeyDown(KeyCode.Space))Repair();
            attackClock-=dt;
            if(attackClock<=0){ralphColumn=UnityEngine.Random.Range(0,3);SpawnBrick();attackClock=Mathf.Max(.48f,1.65f-State.Level*.09f);}
            float speed=2.2f+Mathf.Min(State.Level,25)*.3f;
            for(int i=bricks.Count-1;i>=0;i--){
                var b=bricks[i];float y=b.Sprite.transform.position.y;float next=y-speed*dt;
                b.Sprite.transform.position+=Vector3.down*(speed*dt);
                for(int f=0;f<4;f++)if(y>=Y(f)&&next<Y(f))State.DamageWindow(Mathf.RoundToInt(b.Sprite.transform.position.x/2.1f)+1,f);
                if(Mathf.Abs(b.Sprite.transform.position.x-X(State.Column))<.6f && y>=Y(State.Floor)-.4f && next<=Y(State.Floor)+.45f){if(State.Hit())Tone(130,.16f);Destroy(b.Sprite.gameObject);bricks.RemoveAt(i);}
                else if(next< -4.5f){Destroy(b.Sprite.gameObject);bricks.RemoveAt(i);}
            }
            RefreshVisuals();
        }
        void Repair(){int level=State.Level;if(State.Repair()){repairFlash=.18f;Tone(State.Level>level?880:520,.08f);if(State.Level>level)ClearBricks();}}
        void SpawnBrick(){var r=Box("Falling brick",X(ralphColumn),3.55f,.5f,.24f,new Color(.95f,.40f,.24f),7);bricks.Add(new Brick{Sprite=r});}
        void ClearBricks(){foreach(var b in bricks)Destroy(b.Sprite.gameObject);bricks.Clear();}
        void Restart(){State.Restart();started=true;paused=false;attackClock=1.5f;moveClock=0;ClearBricks();RefreshVisuals();}
        void RefreshVisuals(){
            for(int c=0;c<3;c++)for(int f=0;f<4;f++)panes[c,f].color=State.Broken[c,f]?new Color(.07f,.12f,.20f):new Color(.55f,.87f,.83f);
            felix.transform.position=new Vector3(X(State.Column),Y(State.Floor),0);
            felix.color=State.Protection>0 && Mathf.FloorToInt(State.Protection*12)%2==0?new Color(1,1,1,.3f):Color.white;
            ralph.transform.position=new Vector3(X(ralphColumn),4.25f,0);
            hammer.transform.position=felix.transform.position+new Vector3(.5f,.35f,0);hammer.enabled=repairFlash>0;
        }
        void Tone(float hz,float duration){if(muted)return;int n=(int)(22050*duration);float[] data=new float[n];for(int i=0;i<n;i++)data[i]=Mathf.Sin(2*Mathf.PI*hz*i/22050f)*.13f*(1-i/(float)n);var clip=AudioClip.Create("Arcade",n,1,22050,false);clip.SetData(data,0);audioSource.PlayOneShot(clip);Destroy(clip,duration+1);}
        void Styles(){if(title!=null)return;title=new GUIStyle(GUI.skin.label){fontSize=32,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};title.normal.textColor=Mint;body=new GUIStyle(GUI.skin.label){fontSize=18,alignment=TextAnchor.MiddleCenter};body.normal.textColor=new Color(.85f,.9f,.96f);small=new GUIStyle(body){fontSize=13};score=new GUIStyle(body){fontSize=19,fontStyle=FontStyle.Bold};button=new GUIStyle(GUI.skin.button){fontSize=17,padding=new RectOffset(14,14,10,10)};}
        void OnGUI(){
            Styles();float scale=Mathf.Min(Screen.width/960f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-960*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            GUI.Label(new Rect(0,12,960,40),"FIX-IT FELIX JR.",title);GUI.Label(new Rect(0,50,960,25),"UNITY 2D  /  THE WINDOW REPAIR SHIFT",small);
            GUI.Label(new Rect(70,90,210,30),"SCORE  "+State.Score.ToString("D6"),score);GUI.Label(new Rect(300,90,170,30),"LIVES  "+State.Lives,score);GUI.Label(new Rect(480,90,170,30),"LEVEL  "+State.Level,score);GUI.Label(new Rect(660,90,210,30),"TIME  "+Mathf.CeilToInt(State.TimeLeft),score);
            GUI.Label(new Rect(0,630,960,25),"ARROWS / WASD  move     SPACE  repair     ESC  pause     R  restart     M  sound",small);
            GUI.Label(new Rect(0,658,960,25),State.Remaining+" windows to repair  /  "+(muted?"sound off":"sound on"),small);
            if(!started || paused || State.Over){
                GUI.color=new Color(.035f,.065f,.12f,.97f);GUI.DrawTexture(new Rect(210,225,540,270),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(230,245,500,45),State.Over?"SHIFT COMPLETE":paused?"TAKE A BREATHER":"A BUILDING NEEDS A HERO",title);
                GUI.Label(new Rect(230,303,500,60),State.Over?"Final score: "+State.Score:"Repair every window. Watch for falling bricks.",body);
                if(GUI.Button(new Rect(340,385,280,48),paused?"RESUME":"START SHIFT",button)){if(paused)paused=false;else Restart();}
                GUI.Label(new Rect(230,452,500,26),"Unofficial fan / learning project",small);
            }
        }
        IEnumerator Capture(){
            for(int i=0;i<20;i++)yield return null;
            RefreshVisuals();yield return new WaitForEndOfFrame();
            // Explicit camera target also renders when the Windows player is hidden.
            var camera=Camera.main;var target=new RenderTexture(960,720,24);
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(960,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();
            System.IO.File.WriteAllBytes(capturePath,image.EncodeToPNG());
            Debug.Log("Felix camera capture saved; sprites="+FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length);
            camera.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(image);
            Application.Quit();
        }
    }
}
