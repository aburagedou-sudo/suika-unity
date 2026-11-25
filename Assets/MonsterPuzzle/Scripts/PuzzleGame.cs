using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
namespace MonsterPuzzle {
    public sealed class PuzzleGame : MonoBehaviour {
        enum Mode {
            Solo, Boss, Versus
        }
        sealed class Ball {
            public Rigidbody2D Body;
            public CircleCollider2D Collider;
            public int Tier, Hard, Calm;
            public float Age, Over;
            public bool Settled;
            public float R {
                get {
                    return Collider.radius * 100;
                }
            }
        }
        sealed class Board {
            public float W, H, Line, Drop, Scale, Offset, Aim, Cool, Timer=10, ChainUntil, Countdown=-1;
            public float ShuffleSettleDeadline, ShuffleCooldownUntil, ShuffleGraceUntil, ShuffleVisualUntil;
            public int ShuffleCalmFrames;
            public bool ShuffleSettling;
            public int Turn, Chain, Gauge, Score, Hero, Stage;
            public bool Lost;
            public readonly List<Ball> Balls=new List<Ball>();
            public readonly Queue<int> Forced=new Queue<int>();
            public readonly Queue<Vector2Int> Pending=new Queue<Vector2Int>();
            public GameObject Root;
        }
        Mode mode=Mode.Boss;
        Board player, ai;
        int hero, enemy=1, difficulty=1, bossStage, loop=1, hp, stageScore, best, scene=-1;
        float clock, aiAt, clearAt, cutAt=-100, cutEnd;
        int cutSide, cutStage;
        bool running, finished, aiming, sound=true;
        string cutKey;
        readonly List<int> sequence=new List<int>();
        double seed;
        bool[] defeated=new bool[PuzzleData.StageCount];
        int galleryBoss=-1, galleryImage;
        Texture2D circle;
        Texture2D bgMain;
        Texture2D backgroundOverlay;
        Texture2D panelTexture;
        Texture2D subPanelTexture;
        Texture2D accentPanelTexture;
        Texture2D boardPanelTexture;
        Texture2D buttonTexture;
        Texture2D buttonHoverTexture;
        Texture2D buttonActiveTexture;
        Texture2D buttonDisabledTexture;
        Texture2D titlePanelTexture;
        Texture2D overlayTexture;
        GUIStyle titleStyle;
        GUIStyle sectionStyle;
        GUIStyle labelStyle;
        GUIStyle smallLabelStyle;
        GUIStyle centeredLabelStyle;
        GUIStyle warningLabelStyle;
        GUIStyle panelStyle;
        GUIStyle subPanelStyle;
        GUIStyle boardPanelStyle;
        GUIStyle buttonStyle;
        GUIStyle compactButtonStyle;
        GUIStyle badgeStyle;
        GUIStyle overlayStyle;
        GUIStyle scrollLabelStyle;
        bool stylesReady;
        readonly Dictionary<string, Texture2D> loadedTextureCache=new Dictionary<string, Texture2D>();
        readonly Dictionary<string, Texture2D> renderedBallCache=new Dictionary<string, Texture2D>();
        readonly List<Texture2D> generatedTextures=new List<Texture2D>();
        Vector2 rulesScroll;
        AudioSource audioSource;
        float voiceAt=-100;
        public event Action<int, int> EvolutionVoice;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
            if(FindObjectOfType<PuzzleGame>()==null) new GameObject("MonsterPuzzle").AddComponent<PuzzleGame>();
        }
        
void Awake(){
    audioSource=gameObject.AddComponent<AudioSource>();
    circle=CreateCircleTexture(128);
    CreateUiResources();
    bgMain=LoadImageTexture("assets/bg_main.png");
    StartGame(true);
}
        
        void OnDestroy(){
            if(player!=null) Destroy(player.Root);
            if(ai!=null) Destroy(ai.Root);
            foreach(var tex in generatedTextures)DestroyGeneratedTexture(tex);
            generatedTextures.Clear();
            renderedBallCache.Clear();
            loadedTextureCache.Clear();
        }
        static int Spawn(float x){
            int[] w={
                30, 26, 21, 14, 9
            }
            ;
            for(int i=0; i<5; i++){
                x-=w[i];
                if(x<0)return i;
            }
            return 4;
        }
        static int DrawTier(){
            return Spawn(Random.value*100);
        }
        int Peek(Board b, int n){
            if(n<b.Forced.Count)return b.Forced.ToArray()[n];
            int k=b.Turn+n-b.Forced.Count;
            while(sequence.Count<=k){
                seed=(seed*1103515245+12345)%2147483648;
                sequence.Add(Spawn((float)(seed/2147483648*100)));
            }
            return sequence[k];
        }
        int Consume(Board b){
            if(b.Forced.Count>0)return b.Forced.Dequeue();
            int t=Peek(b, 0);
            b.Turn++;
            return t;
        }
        Board NewBoard(int h, float offset){
            bool v=mode==Mode.Versus;
            var b=new Board{
                W=v?340:460, H=v?460:620, Line=v?84:118, Drop=v?62:88, Scale=v?.72f:1, Offset=offset, Hero=h, Root=new GameObject("Board "+offset)
            }
            ;
            b.Aim=b.W/2;
            Wall(b, new Vector2(-5, b.H/2), new Vector2(10, b.H+20));
            Wall(b, new Vector2(b.W+5, b.H/2), new Vector2(10, b.H+20));
            Wall(b, new Vector2(b.W/2, b.H+5), new Vector2(b.W, 10));
            Wall(b, new Vector2(b.W/2, -5), new Vector2(b.W, 10));
            return b;
        }
        Vector2 World(Board b, Vector2 p){
            return new Vector2(b.Offset+p.x*.01f, -p.y*.01f);
        }
        Vector2 Pos(Board b, Ball p){
            return new Vector2((p.Body.position.x-b.Offset)*100, -p.Body.position.y*100);
        }
        void Wall(Board b, Vector2 p, Vector2 size){
            var g=new GameObject("Wall");
            g.transform.SetParent(b.Root.transform);
            g.transform.position=World(b, p);
            var c=g.AddComponent<BoxCollider2D>();
            c.size=size*.01f;
        }
        void ResetBoard(Board b){
            foreach(var p in b.Balls.ToArray()) Remove(b, p);
            b.Forced.Clear();
            b.Turn=0;
            b.Cool=0;
            b.Timer=10;
            b.Chain=0;
            b.ChainUntil=0;
            b.Gauge=0;
            b.Countdown=-1;
            b.Stage=0;
            b.Lost=false;
            b.Pending.Clear();
            b.Aim=b.W/2;
            b.ShuffleSettling=false;
            b.ShuffleSettleDeadline=0;
            b.ShuffleCooldownUntil=0;
            b.ShuffleGraceUntil=0;
            b.ShuffleVisualUntil=0;
            b.ShuffleCalmFrames=0;
        }
        int MaxHp(){
            return Mathf.RoundToInt(PuzzleData.BossHp[bossStage]*(1+.4f*(loop-1)));
        }
        void StartGame(bool restart){
            if(player!=null){
                player.Root.SetActive(false);
                Destroy(player.Root);
            }
            if(ai!=null){
                ai.Root.SetActive(false);
                Destroy(ai.Root);
            }
            sequence.Clear();
            seed=Random.Range(1, 2147483647);
            player=NewBoard(hero, 0);
            ai=mode==Mode.Versus?NewBoard(enemy, 20):null;
            if(restart){
                bossStage=0;
                loop=1;
                stageScore=0;
            }
            player.Score=stageScore;
            hp=MaxHp();
            scene=-1;
            finished=false;
            running=true;
            aiming=false;
            aiAt=clock;
            galleryBoss=-1;
        }
        Ball Add(Board b, int t, Vector2 at, int hard=0, bool settled=false){
            var g=new GameObject("Ball");
            g.transform.SetParent(b.Root.transform);
            g.transform.position=World(b, at);
            var rb=g.AddComponent<Rigidbody2D>();
            rb.gravityScale=21/9.81f;
            rb.constraints=RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var c=g.AddComponent<CircleCollider2D>();
            var mat=new PhysicsMaterial2D("Ball") {
                friction=.06f, bounciness=.18f
            }
            ;
            c.sharedMaterial=mat;
            var p=new Ball{
                Body=rb, Collider=c, Hard=hard, Settled=settled
            }
            ;
            SetTier(b, p, t);
            b.Balls.Add(p);
            return p;
        }
        void SetTier(Board b, Ball p, int t){
            p.Tier=t;
            p.Collider.radius=PuzzleData.Radii[t]*b.Scale*.01f;
            p.Body.mass=Mathf.PI*p.Collider.radius*p.Collider.radius;
        }
        void Remove(Board b, Ball p){
            if(!b.Balls.Remove(p))return;
            p.Body.simulated=false;
            Destroy(p.Collider.sharedMaterial);
            Destroy(p.Body.gameObject);
        }
        float Fill(Board b){
            return Mathf.Clamp01(b.Balls.Where(p=>p.Settled).Sum(p=>Mathf.PI*p.R*p.R)/(b.W*(b.H-b.Line)));
        }
        void Shuffle(Board b)
        {
            // 対戦は従来どおり、プレイヤー側の現在玉とNEXTだけを引き直す。
            if (mode == Mode.Versus)
            {
                b.Forced.Clear();
                b.Forced.Enqueue(DrawTier());
                b.Forced.Enqueue(DrawTier());
                return;
            }
            // ソロ／ボス制圧戦は盤面全体を揺らし、全ピースに強い上向きの勢いを与える。
            if (!running || finished || scene >= 0 || b.Balls.Count == 0 ||
            b.ShuffleSettling || clock < b.ShuffleCooldownUntil)
            {
                return;
            }
            foreach (Ball piece in b.Balls)
            {
                float sizeFactor = 1f - 0.25f * (piece.R / PuzzleData.Radii[10]);
                float horizontal = Random.Range(-6.4f, 6.4f) +
                (Pos(b, piece).x - b.W / 2f) * 0.011f;
                float vertical = Random.Range(5.6f, 9.8f) +
                piece.R * 0.024f;
                piece.Body.velocity += new Vector2(horizontal, vertical) * sizeFactor;
                piece.Calm = 0;
                piece.Settled = false;
                piece.Over = 0f;
            }
            b.ShuffleSettling = true;
            b.ShuffleSettleDeadline = clock + 4f;
            b.ShuffleGraceUntil = clock + 3f;
            b.ShuffleVisualUntil = clock + 0.72f;
            b.ShuffleCalmFrames = 0;
            b.Countdown = -1;
            Play("assets/se_shuffle");
        }
        void Drop(Board b){
            if(!running||finished||scene>=0||b.Cool>0||b.ShuffleSettling)return;
            int t=Consume(b);
            float r=PuzzleData.Radii[t]*b.Scale;
            Add(b, t, new Vector2(Mathf.Clamp(b.Aim, r+(ai!=null?3:4), b.W-r-(ai!=null?3:4)), b.Drop));
            b.Cool=(ai!=null?22:26)/60f;
            b.Timer=10;
            if(b.Pending.Count>0&&Fill(b)<.8f){
                var q=b.Pending.Dequeue();
                r=PuzzleData.Radii[q.x]*b.Scale;
                var p=Add(b, q.x, new Vector2(Random.Range(r+6, b.W-r-6), r+3), q.y);
                p.Body.velocity=new Vector2(0, -.3f);
            }
            Play("assets/se_drop");
        }
        static string AudioResourcePath(string key){
            const string voicePrefix = "assets/voice/";
            const string assetsPrefix = "assets/";
            if(key.StartsWith(voicePrefix, StringComparison.Ordinal))
                return "music/" + key.Substring(voicePrefix.Length);
            if(key.StartsWith(assetsPrefix, StringComparison.Ordinal))
                return "music/" + key.Substring(assetsPrefix.Length);
            return key;
        }
        
static string ImageResourcePath(string key){
    const string assetsPrefix = "assets/";
    if(!key.StartsWith(assetsPrefix, StringComparison.Ordinal))
        return key;

    string assetName = key.Substring(assetsPrefix.Length);
    if(assetName.StartsWith("kaiju_", StringComparison.Ordinal))
        return "item/" + assetName;
    if(assetName.StartsWith("ojama_", StringComparison.Ordinal))
        return "item/" + assetName;
    if(assetName.StartsWith("mecha", StringComparison.Ordinal))
        return "stand/" + assetName;
    return "cg/" + assetName;
}
        void Play(string key){
            if(!sound)return;
            var clip=Resources.Load<AudioClip>(AudioResourcePath(key));
            if(clip!=null)audioSource.PlayOneShot(clip);
        }
        void Voice(Board b, int t){
            if(EvolutionVoice!=null)EvolutionVoice(b.Hero, t+1);
            if(clock-voiceAt>=1){
                voiceAt=clock;
                Play("assets/voice/evo_"+Mathf.Clamp(Mathf.CeilToInt((t+1)/2f), 1, 6));
            }
            Play("assets/se_merge");
        }
        void Chain(Board b){
            b.Chain=clock>b.ChainUntil?1:b.Chain+1;
            b.ChainUntil=clock+1.1f;
        }
        void Send(Board b, int t, bool vanish){
            Board target=b==player?ai:player;
            if(target==null)return;
            int extra=Mathf.Max(0, b.Chain-1);
            if(vanish||t>=7){
                target.Pending.Enqueue(new Vector2Int(2, 2));
                for(int i=1; i<Mathf.Max(2, 1+(vanish?10:t)-4)+extra; i++)target.Pending.Enqueue(new Vector2Int(2, 1));
            }
            else if(t>=4){
                int sz=t-4;
                target.Pending.Enqueue(new Vector2Int(sz, 2));
                for(int i=0; i<1+extra; i++)target.Pending.Enqueue(new Vector2Int(sz, 1));
            }
            else for(int i=0; i<extra; i++){
                float u=Random.value;
                target.Pending.Enqueue(new Vector2Int(u<.6f?0:u<.9f?1:2, 1));
            }
        }
        void Soften(Board b, Vector2 at, float r, int amount){
            foreach(var p in b.Balls.ToArray()){
                if(p.Hard==0||Vector2.Distance(Pos(b, p), at)>r+p.R+8)continue;
                if(p.Hard==1)Remove(b, p);
                else p.Hard=Mathf.Max(1, p.Hard-amount);
            }
        }
        void Evolution(Board b, int t, bool vanish, Vector2 at, float r, int amount){
            Voice(b, t);
            if(ai!=null){
                b.Gauge=Mathf.Min(100, b.Gauge+(vanish?16:2+t));
                Soften(b, at, r, amount);
                Send(b, t, vanish);
            }
            else{
                b.Score+=vanish?500:(t+1)*(t+2)/2*10;
                best=Mathf.Max(best, b.Score);
                if(mode==Mode.Boss){
                    hp=Mathf.Max(0, hp-(vanish?90:PuzzleData.Damage[t]));
                    if(hp==0){
                        defeated[bossStage]=true;
                        scene=0;
                        clearAt=clock+(bossStage==PuzzleData.StageCount-1?5.2f:3.6f);
                        foreach(var p in b.Balls.ToArray())Remove(b, p);
                    }
                }
            }
        }
        void Resolve(Board b){
            var snapshot=b.Balls.ToArray();
            var used=new HashSet<Ball>();
            var pop=new HashSet<Ball>();
            var pairs=new List<Ball[]>();
            for(int i=0; i<snapshot.Length; i++)for(int j=i+1; j<snapshot.Length; j++){
                var a=snapshot[i];
                var q=snapshot[j];
                if(a.Tier!=q.Tier||Vector2.Distance(Pos(b, a), Pos(b, q))>a.R+q.R+.7f)continue;
                if(a.Hard==1&&q.Hard==0)pop.Add(a);
                if(q.Hard==1&&a.Hard==0)pop.Add(q);
                if(a.Hard!=0||q.Hard!=0||used.Contains(a)||used.Contains(q))continue;
                used.Add(a);
                used.Add(q);
                pairs.Add(new[]{
                    a, q
                }
                );
            }
            foreach(var p in pop)Remove(b, p);
            if(pairs.Count>0)Chain(b);
            foreach(var pair in pairs){
                if(scene>=0)break;
                var a=pair[0];
                var q=pair[1];
                var at=(Pos(b, a)+Pos(b, q))/2;
                var vel=(a.Body.velocity+q.Body.velocity)/2;
                int t=a.Tier;
                Remove(b, a);
                Remove(b, q);
                bool vanish=t==10;
                if(!vanish){
                    t++;
                    var n=Add(b, t, at, 0, true);
                    n.Body.velocity=vel;
                }
                Evolution(b, t, vanish, at, PuzzleData.Radii[t]*b.Scale, 2);
            }
        }
        void TickBoard(Board b, float dt){
            b.Cool=Mathf.Max(0, b.Cool-dt);
            if(ai!=null){
                b.Timer-=dt;
                if(b.Timer<=0)Drop(b);
            }
            Resolve(b);
            if (scene >= 0) return;
            if (b.ShuffleSettling)
            {
                bool allCalm = b.Balls.Count > 0;
                foreach (Ball piece in b.Balls)
                {
                    Vector2 velocity = piece.Body.velocity;
                    if (Mathf.Abs(velocity.x) > 0.55f || Mathf.Abs(velocity.y) > 0.55f)
                    {
                        allCalm = false;
                        break;
                    }
                }
                b.ShuffleCalmFrames = allCalm ? b.ShuffleCalmFrames + 1 : 0;
                if (b.ShuffleCalmFrames >= 25 || clock >= b.ShuffleSettleDeadline)
                {
                    b.ShuffleSettling = false;
                    b.ShuffleCalmFrames = 0;
                    b.ShuffleCooldownUntil = clock + 4f;
                    b.ShuffleGraceUntil = Mathf.Max(b.ShuffleGraceUntil, clock + 1.8f);
                }
            }
            bool shuffleGrace = b.ShuffleSettling || clock < b.ShuffleGraceUntil;
            float max=0;
            foreach(var p in b.Balls){
                p.Age+=dt;
                var v=p.Body.velocity;
                if(v.y< -9.8f)p.Body.velocity=new Vector2(v.x, -9.8f);
                if(!p.Settled){
                    if(Mathf.Abs(v.x)<.5f&&Mathf.Abs(v.y)<.5f)p.Calm++;
                    else p.Calm=0;
                    if(p.Age>=2.5f||(p.Age>=8/60f&&p.Calm>=6))p.Settled=true;
                }
                if (shuffleGrace) p.Over=0;
                else if(p.Hard==0&&p.Age>=(ai!=null?80:96)/60f&&Pos(b, p).y-p.R<b.Line&&Mathf.Abs(v.y)<1.3f)p.Over+=dt;
                else p.Over=0;
                max=Mathf.Max(max, p.Over);
            }
            b.Countdown=shuffleGrace?-1:(max>0?10-max:-1);
            if(!shuffleGrace&&max>=10){
                b.Lost=true;
                b.Stage=3;
                finished=true;
                aiming=false;
                foreach(var p in player.Balls)p.Body.simulated=false;
                if(ai!=null)foreach(var p in ai.Balls)p.Body.simulated=false;
                return;
            }
            float f=Fill(b);
            int st=f>=.8f?2:f>.4f?1:0;
            if(st>b.Stage&&clock-cutAt>=20){
                cutAt=clock;
                cutEnd=clock+1.5f;
                cutSide=b==player?0:1;
                cutStage=st;
                cutKey=HeroKey(b.Hero, st);
            }
            b.Stage=st;
        }
        void FixedUpdate(){
            clock+=Time.fixedDeltaTime;
            if(!running||finished||galleryBoss>=0)return;
            if(scene>=0){
                if(scene==3&&clock>=clearAt){
                    stageScore=player.Score;
                    bossStage++;
                    if(bossStage>=PuzzleData.StageCount){
                        bossStage=0;
                        loop++;
                    }
                    ResetBoard(player);
                    hp=MaxHp();
                    scene=-1;
                }
                return;
            }
            TickBoard(player, Time.fixedDeltaTime);
            if(ai!=null&&!finished){
                TickBoard(ai, Time.fixedDeltaTime);
                if(!finished&&clock>=aiAt){
                    aiAt=clock+new[]{
                        2.3f, 1.5f,.95f
                    }
                    [difficulty];
                    if(ai.Gauge>=100)Special(ai, null, true);
                    ai.Aim=Choose(ai);
                    Drop(ai);
                }
            }
        }
        float Choose(Board b){
            float r=PuzzleData.Radii[Peek(b, 0)]*b.Scale;
            float bestX=b.W/2, bs=-1e9f;
            for(int i=0; i<=14; i++){
                float x=r+4+(b.W-2*r-8)*i/14;
                float top=b.H, bonus=0;
                foreach(var p in b.Balls){
                    var at=Pos(b, p);
                    if(Mathf.Abs(at.x-x)<p.R+r*.9f){
                        top=Mathf.Min(top, at.y-p.R);
                        if(p.Hard<2&&p.Tier==Peek(b, 0))bonus=900;
                    }
                }
                float sc=bonus-(b.H-top)*2.2f+Random.value*30-(top<b.Line+60?500:0);
                if(sc>bs){
                    bs=sc;
                    bestX=x;
                }
            }
            return Random.value>new[]{
                .35f,.7f,.95f
            }
            [difficulty]?Random.Range(r+4, b.W-r-4):bestX;
        }
        List<Ball> Norm(Board b){
            return b.Balls.Where(p=>p.Hard==0).ToList();
        }
        static void Mix<T>(IList<T> list){
            for(int i=list.Count-1; i>0; i--){
                int j=Random.Range(0, i+1);
                T t=list[i];
                list[i]=list[j];
                list[j]=t;
            }
        }
        void Evolve(Board b, Ball p){
            var at=Pos(b, p);
            bool vanish=p.Tier==10;
            int t=vanish?10:p.Tier+1;
            if(vanish)Remove(b, p);
            else{
                SetTier(b, p, t);
                p.Settled=true;
            }
            Evolution(b, t, vanish, at, PuzzleData.Radii[t]*b.Scale, 1);
        }
        Ball Nearest(Board b, Vector2 pt){
            return Norm(b).Where(p=>Vector2.Distance(Pos(b, p), pt)<=p.R+16).OrderBy(p=>Vector2.Distance(Pos(b, p), pt)).FirstOrDefault();
        }
        void Special(Board b, Vector2? point, bool automatic=false){
            if(finished||b.Gauge<100)return;
            int id=b.Hero;
            var list=Norm(b);
            var pick=list.Count>0?list[Random.Range(0, list.Count)]:null;
            if(automatic&&pick!=null)point=Pos(b, pick);
            if(!automatic&&(id==2||id==3)&&!point.HasValue){
                aiming=!aiming;
                return;
            }
            if(!automatic&&id==2&&point.HasValue&&Nearest(b, point.Value)==null)return;
            b.Gauge=0;
            if(b==player)aiming=false;
            Vector2 at=pick!=null?Pos(b, pick):Vector2.zero;
            switch(id){
                case 0:if(pick!=null)foreach(var p in list.Where(p=>Mathf.Abs(Pos(b, p).y-at.y)<p.R).ToArray())Remove(b, p);
                break;
                case 1:if(pick!=null){
                    var near=new List<Ball>();
                    foreach(var p in list){
                        float d=Vector2.Distance(Pos(b, p), at);
                        if(d<80+p.R*.4f)Remove(b, p);
                        else if(d<185)near.Add(p);
                    }
                    var ts=near.Select(p=>p.Tier).ToList();
                    Mix(ts);
                    for(int i=0; i<near.Count; i++){
                        var p=near[i];
                        SetTier(b, p, ts[i]);
                        var v=(Pos(b, p)-at).normalized*4;
                        p.Body.velocity+=new Vector2(v.x, -v.y+1.4f);
                    }
                }
                break;
                case 2:if(point.HasValue){
                    var p=Nearest(b, point.Value);
                    if(p!=null){
                        Chain(b);
                        Evolve(b, p);
                    }
                }
                break;
                case 3:if(point.HasValue){
                    var hits=list.Where(p=>Mathf.Abs(Pos(b, p).x-point.Value.x)<p.R).ToArray();
                    if(hits.Length>0)Chain(b);
                    foreach(var p in hits)Evolve(b, p);
                }
                break;
                case 4:foreach(var p in b.Balls.Where(p=>p.Hard>0).ToArray()){
                    p.Hard=1;
                    Remove(b, p);
                }
                break;
                case 5:Mix(list);
                foreach(var p in list.Take(5))Remove(b, p);
                break;
                case 6:var big=list.OrderByDescending(p=>p.Tier).ThenBy(p=>Pos(b, p).y).FirstOrDefault();
                if(big!=null)Remove(b, big);
                break;
                case 7:if(pick!=null){
                    Remove(b, pick);
                    foreach(var p in Norm(b).Where(p=>Vector2.Distance(Pos(b, p), at)<92+p.R*.4f).ToArray()){
                        if(p.Tier==0)Remove(b, p);
                        else SetTier(b, p, p.Tier-1);
                    }
                }
                break;
                case 8:if(list.Count>0){
                    int min=list.Min(p=>p.Tier);
                    Chain(b);
                    foreach(var p in list.Where(p=>p.Tier==min).ToArray())Evolve(b, p);
                }
                break;
                case 9:foreach(var p in list)p.Body.velocity+=new Vector2(Random.Range(-5.4f, 5.4f), Random.Range(2.2f, 6f));
                Mix(list);
                for(int i=0; i+1<Mathf.Min(6, list.Count); i+=2){
                    int t=list[i].Tier;
                    SetTier(b, list[i], list[i+1].Tier);
                    SetTier(b, list[i+1], t);
                }
                break;
            }
        }
        static string HeroKey(int h, int st){
            return "assets/mecha"+(h+1).ToString("00")+"_"+new[]{
                "normal", "mid", "heavy", "down"
            }
            [st]+".png";
        }
        
        static string StripExtension(string resourcePath){
            int extension = resourcePath.LastIndexOf('.');
            return extension >= 0 ? resourcePath.Substring(0, extension) : resourcePath;
        }
        void DestroyGeneratedTexture(Texture2D tex){
            if(tex!=null) Destroy(tex);
        }
        Texture2D TrackTexture(Texture2D tex){
            generatedTextures.Add(tex);
            return tex;
        }
        Texture2D CreateCircleTexture(int size){
            var tex=TrackTexture(new Texture2D(size, size, TextureFormat.RGBA32, false));
            float c=(size-1)*0.5f;
            float r=c;
            for(int y=0; y<size; y++)for(int x=0; x<size; x++){
                float dx=x-c;
                float dy=y-c;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Sqrt(dx*dx+dy*dy)<=r?1:0));
            }
            tex.filterMode=FilterMode.Bilinear;
            tex.Apply();
            return tex;
        }
        Texture2D CreateVerticalGradientTexture(int width, int height, Color top, Color bottom, Color border){
            var tex=TrackTexture(new Texture2D(width, height, TextureFormat.RGBA32, false));
            for(int y=0; y<height; y++){
                float t=height<=1?0:(float)y/(height-1);
                Color row=Color.Lerp(top, bottom, t);
                for(int x=0; x<width; x++){
                    bool edge=x<2||y<2||x>=width-2||y>=height-2;
                    tex.SetPixel(x, y, edge?border:row);
                }
            }
            tex.filterMode=FilterMode.Bilinear;
            tex.Apply();
            return tex;
        }
        void CreateUiResources(){
            if(stylesReady)return;
            backgroundOverlay=CreateVerticalGradientTexture(8, 128, new Color(.06f, .08f, .16f, .08f), new Color(.02f, .03f, .08f, .45f), new Color(0, 0, 0, 0));
            panelTexture=CreateVerticalGradientTexture(32, 32, new Color(.08f, .10f, .20f, .94f), new Color(.04f, .06f, .13f, .94f), new Color(.42f, .58f, .82f, .95f));
            subPanelTexture=CreateVerticalGradientTexture(32, 32, new Color(.10f, .12f, .24f, .88f), new Color(.05f, .07f, .16f, .90f), new Color(.26f, .38f, .64f, .90f));
            accentPanelTexture=CreateVerticalGradientTexture(32, 32, new Color(.18f, .12f, .30f, .96f), new Color(.08f, .07f, .18f, .96f), new Color(.84f, .63f, .32f, .96f));
            boardPanelTexture=CreateVerticalGradientTexture(32, 64, new Color(.06f, .09f, .18f, .96f), new Color(.02f, .03f, .08f, .97f), new Color(.30f, .45f, .74f, .95f));
            buttonTexture=CreateVerticalGradientTexture(32, 32, new Color(.31f, .41f, .82f, .98f), new Color(.16f, .24f, .61f, .98f), new Color(.88f, .92f, 1f, 1f));
            buttonHoverTexture=CreateVerticalGradientTexture(32, 32, new Color(.44f, .55f, .96f, 1f), new Color(.22f, .32f, .76f, 1f), new Color(1f, 1f, 1f, 1f));
            buttonActiveTexture=CreateVerticalGradientTexture(32, 32, new Color(.22f, .30f, .63f, 1f), new Color(.10f, .15f, .44f, 1f), new Color(.92f, .78f, .42f, 1f));
            titlePanelTexture=CreateVerticalGradientTexture(32, 48, new Color(.18f, .11f, .32f, .98f), new Color(.07f, .08f, .18f, .98f), new Color(.96f, .80f, .42f, 1f));
            overlayTexture=CreateVerticalGradientTexture(16, 32, new Color(0f, 0f, 0f, .78f), new Color(.06f, .07f, .13f, .88f), new Color(.88f, .92f, 1f, .30f));

            titleStyle=new GUIStyle(GUI.skin.label){ fontSize=30, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, normal={textColor=new Color(1f, .96f, .82f)}, padding=new RectOffset(12, 12, 8, 8) };
            sectionStyle=new GUIStyle(GUI.skin.label){ fontSize=15, fontStyle=FontStyle.Bold, normal={textColor=new Color(.95f, .97f, 1f)}, padding=new RectOffset(4, 4, 2, 4) };
            labelStyle=new GUIStyle(GUI.skin.label){ fontSize=14, wordWrap=true, normal={textColor=new Color(.94f, .97f, 1f)} };
            smallLabelStyle=new GUIStyle(labelStyle){ fontSize=12 };
            centeredLabelStyle=new GUIStyle(labelStyle){ alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
            warningLabelStyle=new GUIStyle(labelStyle){ alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold, normal={textColor=new Color(1f, .80f, .65f)} };
            panelStyle=new GUIStyle(GUI.skin.box){ normal={background=panelTexture, textColor=Color.white}, border=new RectOffset(4, 4, 4, 4), padding=new RectOffset(12, 12, 12, 12) };
            subPanelStyle=new GUIStyle(GUI.skin.box){ normal={background=subPanelTexture, textColor=Color.white}, border=new RectOffset(4, 4, 4, 4), padding=new RectOffset(10, 10, 10, 10) };
            boardPanelStyle=new GUIStyle(GUI.skin.box){ normal={background=boardPanelTexture, textColor=Color.white}, border=new RectOffset(4, 4, 4, 4), padding=new RectOffset(8, 8, 8, 8) };
            buttonStyle=new GUIStyle(GUI.skin.button){
                fontSize=14,
                fontStyle=FontStyle.Bold,
                alignment=TextAnchor.MiddleCenter,
                normal={background=buttonTexture, textColor=Color.white},
                hover={background=buttonHoverTexture, textColor=Color.white},
                active={background=buttonActiveTexture, textColor=Color.white},
                focused={background=buttonHoverTexture, textColor=Color.white},
                onNormal={background=buttonActiveTexture, textColor=Color.white},
                padding=new RectOffset(10, 10, 8, 8),
                border=new RectOffset(4, 4, 4, 4)
            };
            compactButtonStyle=new GUIStyle(buttonStyle){ fontSize=13, padding=new RectOffset(8, 8, 6, 6) };
            badgeStyle=new GUIStyle(GUI.skin.box){ fontSize=14, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, normal={background=accentPanelTexture, textColor=Color.white}, border=new RectOffset(4, 4, 4, 4), padding=new RectOffset(8, 8, 6, 6) };
            overlayStyle=new GUIStyle(GUI.skin.box){ fontSize=22, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, normal={background=overlayTexture, textColor=Color.white}, wordWrap=true, padding=new RectOffset(20, 20, 18, 18), border=new RectOffset(4, 4, 4, 4) };
            scrollLabelStyle=new GUIStyle(labelStyle){ margin=new RectOffset(4, 4, 4, 4) };
            stylesReady=true;
        }
        Texture2D LoadImageTexture(string key){
            string resourcePath=StripExtension(ImageResourcePath(key));
            Texture2D tex;
            if(!loadedTextureCache.TryGetValue(resourcePath, out tex)){
                tex=Resources.Load<Texture2D>(resourcePath);
                loadedTextureCache[resourcePath]=tex;
            }
            return tex;
        }
        Color TierColor(int tier){
            Color color;
            ColorUtility.TryParseHtmlString(PuzzleData.Colors[Mathf.Clamp(tier, 0, PuzzleData.Colors.Length-1)], out color);
            return color;
        }
        static string KaijuImageKey(int tier){
            return "assets/kaiju_" + PuzzleData.Keys[tier] + ".png";
        }
        static string OjamaImageKey(int hard, int sizeTier){
            if(hard==1)return "assets/ojama_bubble.png";
            return "assets/ojama_purple_" + (Mathf.Clamp(sizeTier, 0, 2)+1) + ".png";
        }
        int BossHeroIndex(){
            return bossStage%PuzzleData.StageCount;
        }
        int BossVisualState(){
            if(MaxHp()<=0)return 3;
            float remain=hp/(float)MaxHp();
            if(remain>.75f)return 0;
            if(remain>.5f)return 1;
            if(remain>.25f)return 2;
            return 3;
        }
        string BossStandKey(){
            return HeroKey(BossHeroIndex(), BossVisualState());
        }
        string BossSceneKey(int bossIndex, int sceneIndex){
            return "assets/scenes/scene_char"+(bossIndex+1).ToString("00")+"_"+PuzzleData.Keys[bossIndex%PuzzleData.Keys.Length]+"_"+new[]{ "start", "mid", "after" }[Mathf.Clamp(sceneIndex, 0, 2)]+".jpg";
        }
        Texture2D BuildBallTexture(Texture2D source, Color ringColor, float imageAlpha, float brightness){
            if(source==null)return null;
            int size=192;
            float center=(size-1)*0.5f;
            float outerRadius=center-1f;
            float innerRadius=outerRadius*.93f;
            float square=Mathf.Min(source.width, source.height);
            float offsetX=(source.width-square)*0.5f;
            float offsetY=(source.height-square)*0.5f;
            var tex=TrackTexture(new Texture2D(size, size, TextureFormat.RGBA32, false));
            for(int y=0; y<size; y++)for(int x=0; x<size; x++){
                float dx=(x+.5f)-center;
                float dy=(y+.5f)-center;
                float distance=Mathf.Sqrt(dx*dx+dy*dy);
                Color pixel;
                if(distance>outerRadius)pixel=new Color(0, 0, 0, 0);
                else if(distance>innerRadius)pixel=ringColor;
                else{
                    float u=Mathf.Clamp01((dx/innerRadius+1f)*.5f);
                    float v=Mathf.Clamp01((dy/innerRadius+1f)*.5f);
                    float su=(offsetX+u*(square-1f))/Mathf.Max(1f, source.width-1f);
                    float sv=(offsetY+v*(square-1f))/Mathf.Max(1f, source.height-1f);
                    Color sample=source.GetPixelBilinear(su, sv);
                    sample.r*=brightness;
                    sample.g*=brightness;
                    sample.b*=brightness;
                    sample.a*=imageAlpha;
                    pixel=Color.Lerp(Color.white, sample, Mathf.Clamp01(sample.a));
                    pixel.a=1f;
                }
                tex.SetPixel(x, y, pixel);
            }
            tex.filterMode=FilterMode.Bilinear;
            tex.Apply();
            return tex;
        }
        Texture2D GetRenderedBallTexture(int tier, int hard){
            string cacheKey=tier+":"+hard;
            Texture2D texture;
            if(renderedBallCache.TryGetValue(cacheKey, out texture))return texture;
            Texture2D source=null;
            Color ring=TierColor(tier);
            float alpha=1f;
            float brightness=1f;
            if(hard==0)source=LoadImageTexture(KaijuImageKey(tier));
            else if(hard==1){
                source=LoadImageTexture(OjamaImageKey(hard, tier));
                ring.a=.85f;
                alpha=.76f;
            }
            else if(hard==2){
                source=LoadImageTexture(OjamaImageKey(hard, tier));
                ring=new Color(.58f, .22f, .78f, 1f);
            }
            else if(hard==3){
                source=LoadImageTexture(OjamaImageKey(hard, tier));
                ring=new Color(.27f, .08f, .40f, 1f);
                brightness=.55f;
            }
            texture=BuildBallTexture(source, ring, alpha, brightness);
            renderedBallCache[cacheKey]=texture;
            return texture;
        }
        void Image(Rect r, string key){
            Texture2D tex=LoadImageTexture(key);
            if(tex!=null)GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            else GUI.Box(r, key, subPanelStyle);
        }
        void DrawFallbackBall(Rect r, int tier, int hard){
            Color c=TierColor(tier);
            if(hard==1)c.a=.42f;
            if(hard==2)c=new Color(.55f, .22f, .75f);
            if(hard==3)c=new Color(.25f, .08f, .4f);
            var old=GUI.color;
            GUI.color=c;
            GUI.DrawTexture(r, circle);
            GUI.color=old;
            if(hard==0){
                var style=new GUIStyle(centeredLabelStyle){ fontSize=Mathf.Clamp((int)(r.width*.35f), 10, 25) };
                GUI.Label(r, (tier+1).ToString(), style);
            }
        }
        void Dot(Rect r, int tier, int hard){
            Texture2D visual=GetRenderedBallTexture(tier, hard);
            if(visual!=null){
                GUI.DrawTexture(r, visual, ScaleMode.StretchToFill, true);
                return;
            }
            DrawFallbackBall(r, tier, hard);
        }
        void DrawScreenBackground(){
            GUI.depth=100;
            if(bgMain==null)bgMain=LoadImageTexture("assets/bg_main.png");
            if(bgMain!=null)GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgMain, ScaleMode.StretchToFill, true);
            else{
                GUI.color=new Color(.07f, .10f, .18f, 1f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color=Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundOverlay, ScaleMode.StretchToFill, true);
        }
        void DrawBossStatus(Rect area){
            GUI.Box(area, GUIContent.none, panelStyle);
            GUI.Label(new Rect(area.x+14, area.y+10, area.width-28, 28), "敵ボス："+PuzzleData.BossNames[BossHeroIndex()], sectionStyle);
            Image(new Rect(area.x+20, area.y+44, area.width-40, 280), BossStandKey());
            GUI.Label(new Rect(area.x+16, area.y+330, area.width-32, 24), "脱衣段階　"+new[]{ "normal", "mid", "heavy", "down" }[BossVisualState()], centeredLabelStyle);
            GUI.Label(new Rect(area.x+16, area.y+356, area.width-32, 46), "HP "+hp+" / "+MaxHp(), badgeStyle);
            GUI.Label(new Rect(area.x+16, area.y+408, area.width-32, 40), "周回ごとにHPが上昇します。
撃破後は場面CGを閲覧できます。", smallLabelStyle);
        }
        bool StyledButton(Rect rect, string text, bool enabled=true){
            bool old=GUI.enabled;
            GUI.enabled=enabled;
            bool result=GUI.Button(rect, text, compactButtonStyle);
            GUI.enabled=old;
            return result;
        }
        
void DrawBoard(Board b, Rect area, bool human)
{
    if(human&&mode!=Mode.Versus&&clock<b.ShuffleVisualUntil){
        area.x += Mathf.Sin(clock*80f)*5f;
        area.y += Mathf.Cos(clock*91f)*3f;
    }
    float k=area.width/b.W;
    GUI.BeginGroup(area, GUIContent.none, boardPanelStyle);
    GUI.Box(new Rect(0, 0, area.width, area.height), GUIContent.none, boardPanelStyle);
    Image(new Rect(8, 8, area.width-16, area.height-16), HeroKey(b.Hero, b.Stage));
    var old=GUI.color;
    GUI.color=new Color(.08f, .05f, .15f, .52f);
    GUI.DrawTexture(new Rect(8, 8, area.width-16, area.height-16), Texture2D.whiteTexture);
    GUI.color=new Color(1f, .35f, .42f, .95f);
    GUI.DrawTexture(new Rect(10, b.Line*k, area.width-20, 3), Texture2D.whiteTexture);
    GUI.color=old;
    foreach(var p in b.Balls){
        var at=Pos(b, p);
        float r=p.R*k;
        Dot(new Rect(at.x*k-r, at.y*k-r, r*2, r*2), p.Tier, p.Hard);
    }
    if(!finished&&scene<0){
        int t=Peek(b, 0);
        float r=PuzzleData.Radii[t]*b.Scale*k;
        Dot(new Rect(Mathf.Clamp(b.Aim*k, r, area.width-r)-r, b.Drop*k-r, r*2, r*2), t, 0);
    }
    if(b.Countdown>=0)GUI.Label(new Rect(10, b.Line*k+8, area.width-20, 28), "あと "+Mathf.Max(0, b.Countdown).ToString("F1")+" 秒", warningLabelStyle);
    if(finished)GUI.Box(new Rect(36, area.height*.39f, area.width-72, 70), ai==null?"ゲームオーバー":b.Lost?"まけ":"かち", overlayStyle);
    if(human&&!finished&&scene<0&&galleryBoss<0){
        var e=Event.current;
        var pt=e.mousePosition;
        if(new Rect(0, 0, area.width, area.height).Contains(pt)){
            if(e.type==EventType.MouseMove||e.type==EventType.MouseDrag||e.type==EventType.MouseDown||e.type==EventType.MouseUp)b.Aim=pt.x/k;
            if(e.type==EventType.MouseUp&&e.button==0){
                if(aiming)Special(b, pt/k);
                else Drop(b);
                e.Use();
            }
        }
    }
    GUI.EndGroup();
}
        
        void Info(Board b, Rect r, bool human){
            GUI.BeginGroup(r, GUIContent.none, subPanelStyle);
            GUI.Box(new Rect(0, 0, r.width, r.height), GUIContent.none, subPanelStyle);
            if(ai!=null){
                GUI.Label(new Rect(10, 4, r.width-200, 18), (human?"👤 ":"🤖 ")+PuzzleData.Heroes[b.Hero]+"  Lv."+(b.Stage+1)+"/4  "+new[]{ "通常", "中破", "大破", "ダウン" }[b.Stage], smallLabelStyle);
                Dot(new Rect(10, 22, 28, 28), Peek(b, 1), 0);
                GUI.Label(new Rect(44, 20, 140, 16), "NEXT "+PuzzleData.Names[Peek(b, 1)], smallLabelStyle);
                GUI.Label(new Rect(44, 34, 180, 16), "埋まり "+Mathf.RoundToInt(Fill(b)*100)+"% / ゲージ "+b.Gauge+"%", smallLabelStyle);
                GUI.Label(new Rect(190, 20, 120, 16), "⏱ "+Mathf.Max(0, b.Timer).ToString("F1")+" 秒", smallLabelStyle);
                GUI.Label(new Rect(190, 34, 120, 16), "おじゃま "+b.Pending.Count, smallLabelStyle);
                bool canUse=human&&!finished&&b.Gauge>=100;
                if(StyledButton(new Rect(r.width-164, 11, 152, 30), (aiming&&human?"照準中：":"⚡")+PuzzleData.Specials[b.Hero], canUse))Special(b, null);
            }
            else{
                GUI.Label(new Rect(12, 8, r.width-24, 24), (human?"👤 ":"🤖 ")+PuzzleData.Heroes[b.Hero]+"  Lv."+(b.Stage+1)+"/4  "+new[]{ "通常", "中破", "大破", "ダウン" }[b.Stage], sectionStyle);
                GUI.Label(new Rect(12, 30, r.width-24, 22), "盤面の埋まり "+Mathf.RoundToInt(Fill(b)*100)+"%", labelStyle);
                GUI.Label(new Rect(12, 52, 80, 20), "NEXT", sectionStyle);
                Dot(new Rect(94, 44, 44, 44), Peek(b, 1), 0);
                GUI.Label(new Rect(146, 50, r.width-158, 24), PuzzleData.Names[Peek(b, 1)], labelStyle);
            }
            GUI.EndGroup();
        }
        
void OnGUI(){
    if(player==null)return;
    CreateUiResources();
    Matrix4x4 oldMatrix=GUI.matrix;
    DrawScreenBackground();
    GUI.matrix=oldMatrix;

    float scale=Mathf.Min(Screen.width/1100f, Screen.height/800f);
    GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1100*scale)/2, (Screen.height-800*scale)/2, 0), Quaternion.identity, new Vector3(scale, scale, 1));

    GUI.Box(new Rect(10, 5, 1080, 118), GUIContent.none, panelStyle);
    GUI.DrawTexture(new Rect(10, 5, 1080, 40), titlePanelTexture, ScaleMode.StretchToFill, true);
    GUI.Label(new Rect(16, 8, 1068, 30), "モンスター討伐パズル", titleStyle);

    GUILayout.BeginArea(new Rect(18, 46, 1064, 72));
    GUILayout.BeginHorizontal();
    if(GUILayout.Button("ソロ", compactButtonStyle, GUILayout.Width(105))) { mode=Mode.Solo; StartGame(true); }
    if(GUILayout.Button("⚔ ボス制圧戦（1人用）", compactButtonStyle, GUILayout.Width(180))) { mode=Mode.Boss; StartGame(true); }
    if(GUILayout.Button("👙 AI対戦", compactButtonStyle, GUILayout.Width(120))) { mode=Mode.Versus; StartGame(true); }
    if(GUILayout.Button("↺ はじめから", compactButtonStyle, GUILayout.Width(120)))StartGame(true);
    if(GUILayout.Button(sound?"🔊 音 ON":"🔇 音 OFF", compactButtonStyle, GUILayout.Width(110)))sound=!sound;
    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    GUILayout.Label("キャラ", sectionStyle, GUILayout.Width(52));
    if(GUILayout.Button("◀", compactButtonStyle, GUILayout.Width(40))){ hero=(hero+9)%10; player.Hero=hero; }
    GUILayout.Label(PuzzleData.Heroes[hero], labelStyle, GUILayout.Width(90));
    if(GUILayout.Button("▶", compactButtonStyle, GUILayout.Width(40))){ hero=(hero+1)%10; player.Hero=hero; }
    if(ai!=null){
        GUILayout.Space(12);
        if(GUILayout.Button("AI ◀", compactButtonStyle, GUILayout.Width(64))){ enemy=(enemy+9)%10; ai.Hero=enemy; }
        GUILayout.Label(PuzzleData.Heroes[enemy], labelStyle, GUILayout.Width(90));
        if(GUILayout.Button("AI ▶", compactButtonStyle, GUILayout.Width(64))){ enemy=(enemy+1)%10; ai.Hero=enemy; }
        if(GUILayout.Button(new[]{ "弱い", "普通", "強い" }[difficulty], compactButtonStyle, GUILayout.Width(72)))difficulty=(difficulty+1)%3;
    }
    string shuffleLabel = player.ShuffleSettling ? "💥 揺らしてます…" : clock < player.ShuffleCooldownUntil ? "🌀 シャッフル（あと" + Mathf.CeilToInt(player.ShuffleCooldownUntil - clock) + "秒）" : "🌀 シャッフル";
    GUI.enabled = !finished && scene < 0 && !player.ShuffleSettling && clock >= player.ShuffleCooldownUntil && player.Balls.Count > 0;
    GUILayout.FlexibleSpace();
    if(GUILayout.Button(shuffleLabel, compactButtonStyle, GUILayout.Width(210))) Shuffle(player);
    GUI.enabled = true;
    GUILayout.EndHorizontal();
    GUILayout.EndArea();

    GUI.Box(new Rect(10, 126, 1080, 34), GUIContent.none, subPanelStyle);
    string scoreText="SCORE "+player.Score+"    BEST "+best;
    if(mode==Mode.Boss)scoreText += "    STAGE "+(bossStage+1)+" / "+PuzzleData.StageCount+"  ボス："+PuzzleData.BossNames[BossHeroIndex()]+"  HP "+hp+" / "+MaxHp()+"  "+loop+"周目";
    GUI.Label(new Rect(20, 132, 1060, 22), scoreText, sectionStyle);

    if(ai!=null){
        DrawBoard(player, new Rect(55, 170, 420, 568), true);
        DrawBoard(ai, new Rect(625, 170, 420, 568), false);
        Info(player, new Rect(55, 742, 420, 54), true);
        Info(ai, new Rect(625, 742, 420, 54), false);
    }
    else{
        DrawBoard(player, new Rect(30, 170, 460, 620), true);
        Info(player, new Rect(510, 170, 570, 82), true);
        if(mode==Mode.Boss)DrawBossStatus(new Rect(510, 260, 570, 468));
        else GUI.Box(new Rect(510, 260, 570, 468), GUIContent.none, panelStyle);

        GUI.Box(new Rect(510, 540, 570, 250), GUIContent.none, panelStyle);
        GUILayout.BeginArea(new Rect(520, 550, 550, 228));
        rulesScroll=GUILayout.BeginScrollView(rulesScroll);
        for(int i=0; i<11; i++)GUILayout.Label((i+1)+". "+PuzzleData.Names[i]+(i==10?"（最終形態） 合体で消滅 +500点":i==0?" 落下で登場":" 合体 "+((i+1)*(i+2)/2*10)+"点"), scrollLabelStyle);
        GUILayout.Label("通常玉が赤いラインより上に留まり続けると10秒のカウントダウンが進み、0になるとゲームオーバーです（ラインを下回ればその玉のカウントは解除されます。上端に触れただけ・一瞬ラインを越えただけでは負けません）。", scrollLabelStyle);
        if(mode==Mode.Boss){
            GUILayout.Space(8);
            GUILayout.Label("討伐済みCGギャラリー", sectionStyle);
            GUILayout.BeginHorizontal();
            for(int i=0; i<PuzzleData.StageCount; i++)if(defeated[i]&&GUILayout.Button(PuzzleData.BossNames[i], compactButtonStyle, GUILayout.Width(96))){ galleryBoss=i; galleryImage=0; }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    if(finished&&GUI.Button(new Rect(490, 405, 120, 55), mode==Mode.Boss?"やり直す":"もう一度", buttonStyle)){
        if(mode==Mode.Boss)StartGame(false); else StartGame(true);
    }
    if(clock<cutEnd&&cutKey!=null){
        Rect r=new Rect(cutSide==0?10:790, 650, 300, 100);
        GUI.Box(r, GUIContent.none, subPanelStyle);
        Image(new Rect(r.x+8, r.y+8, 96, 84), cutKey);
        GUI.Label(new Rect(r.x+112, r.y+8, 176, 82), new[]{ "通常", "中破！", "大破！" }[cutStage], centeredLabelStyle);
    }
    if(scene>=0&&scene<3){
        GUI.Box(new Rect(230, 118, 640, 610), "⚔ 討伐完了！", overlayStyle);
        string key=BossSceneKey(BossHeroIndex(), scene);
        Image(new Rect(250, 168, 600, 470), key);
        GUI.Label(new Rect(250, 644, 600, 24), "Stage "+(bossStage+1)+" クリア！ "+(scene+1)+" / 3", centeredLabelStyle);
        if(scene>0&&GUI.Button(new Rect(250, 680, 120, 34), "前へ", buttonStyle))scene--;
        if(GUI.Button(new Rect(730, 680, 120, 34), scene==2?"閉じる":"次へ", buttonStyle))scene++;
    }
    if(galleryBoss>=0){
        GUI.Box(new Rect(180, 90, 740, 670), "討伐済みCG："+PuzzleData.BossNames[galleryBoss], overlayStyle);
        Image(new Rect(210, 140, 680, 540), BossSceneKey(galleryBoss, galleryImage));
        if(GUI.Button(new Rect(210, 702, 120, 36), "前へ", buttonStyle))galleryImage=Mathf.Max(0, galleryImage-1);
        if(GUI.Button(new Rect(348, 702, 120, 36), "次へ", buttonStyle))galleryImage=Mathf.Min(2, galleryImage+1);
        if(GUI.Button(new Rect(770, 702, 120, 36), "閉じる", buttonStyle))galleryBoss=-1;
    }
}
        void Update(){
            if(player==null||finished||scene>=0||galleryBoss>=0)return;
            if(Input.GetKey(KeyCode.LeftArrow))player.Aim-=200*Time.deltaTime;
            if(Input.GetKey(KeyCode.RightArrow))player.Aim+=200*Time.deltaTime;
            player.Aim=Mathf.Clamp(player.Aim, 0, player.W);
            if(Input.GetKeyDown(KeyCode.Space)&&!aiming)Drop(player);
        }
    }
}
