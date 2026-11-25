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
        bool[] defeated=new bool[16];
        int galleryBoss=-1, galleryImage;
        Texture2D circle;
        Vector2 rulesScroll;
        AudioSource audioSource;
        float voiceAt=-100;
        public event Action<int, int> EvolutionVoice;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
            if(FindObjectOfType<PuzzleGame>()==null) new GameObject("MonsterPuzzle").AddComponent<PuzzleGame>();
        }
        void Awake(){
            audioSource=gameObject.AddComponent<AudioSource>();
            circle=new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for(int y=0; y<128; y++)for(int x=0; x<128; x++)circle.SetPixel(x, y, new Color(1, 1, 1, Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f))<=63?1:0));
            circle.Apply();
            StartGame(true);
        }
        void OnDestroy(){
            if(player!=null) Destroy(player.Root);
            if(ai!=null) Destroy(ai.Root);
            if(circle!=null) Destroy(circle);
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
        void Image(Rect r, string key){
            string resourcePath = ImageResourcePath(key);
            int extension = resourcePath.LastIndexOf('.');
            if(extension >= 0)resourcePath = resourcePath.Substring(0, extension);
            var tex=Resources.Load<Texture2D>(resourcePath);
            if(tex!=null)GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            else GUI.Box(r, key);
        }
        static string KaijuImageKey(int tier){
            return "assets/kaiju_" + PuzzleData.Keys[tier] + ".png";
        }
        void Dot(Rect r, int tier, int hard){
            // 通常玉は段階に対応する怪獣画像を優先して描画する。
            // お邪魔玉は従来どおり色付きの泡として描画する。
            if(hard==0){
                string resourcePath=ImageResourcePath(KaijuImageKey(tier));
                int extension=resourcePath.LastIndexOf('.');
                if(extension>=0)resourcePath=resourcePath.Substring(0, extension);
                Texture2D kaiju=Resources.Load<Texture2D>(resourcePath);
                if(kaiju!=null){
                    var previousColor=GUI.color;
                    GUI.color=Color.white;
                    GUI.DrawTexture(r, kaiju, ScaleMode.ScaleToFit);
                    GUI.color=previousColor;
                    return;
                }
            }

            Color c;
            ColorUtility.TryParseHtmlString(PuzzleData.Colors[tier], out c);
            if(hard==1)c.a=.42f;
            if(hard==2)c=new Color(.55f,.22f,.75f);
            if(hard==3)c=new Color(.25f,.08f,.4f);
            var old=GUI.color;
            GUI.color=c;
            GUI.DrawTexture(r, circle);
            GUI.color=old;
            if(hard==0){
                var style=new GUIStyle(GUI.skin.label){
                    alignment=TextAnchor.MiddleCenter, fontSize=Mathf.Clamp((int)(r.width*.35f), 10, 25)
                }
                ;
                GUI.Label(r, (tier+1).ToString(), style);
            }
        }
        void DrawBoard(Board b, Rect area, bool human)
        {
            if (human && mode != Mode.Versus && clock < b.ShuffleVisualUntil)
            {
                area.x += Mathf.Sin(clock * 80f) * 5f;
                area.y += Mathf.Cos(clock * 91f) * 3f;
            }
            float k=area.width/b.W;
            GUI.BeginGroup(area);
            GUI.Box(new Rect(0, 0, area.width, area.height), "");
            Image(new Rect(0, 0, area.width, area.height), HeroKey(b.Hero, b.Stage));
            var old=GUI.color;
            GUI.color=new Color(.1f,.05f,.2f,.55f);
            GUI.DrawTexture(new Rect(0, 0, area.width, area.height), Texture2D.whiteTexture);
            GUI.color=Color.red;
            GUI.DrawTexture(new Rect(0, b.Line*k, area.width, 2), Texture2D.whiteTexture);
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
            if(b.Countdown>=0)GUI.Label(new Rect(5, b.Line*k+4, area.width-10, 30), "あと "+Mathf.Max(0, b.Countdown).ToString("F1")+" 秒");
            if(finished)GUI.Box(new Rect(0, area.height*.4f, area.width, 60), ai==null?"ゲームオーバー":b.Lost?"まけ":"かち");
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
            GUILayout.BeginArea(r);
            GUILayout.Label((human?"👤 ":"🤖 ")+PuzzleData.Heroes[b.Hero]+"  Lv."+(b.Stage+1)+"/4 "+new[]{
                "通常", "中破", "大破", "ダウン"
            }
            [b.Stage]+"（盤面の埋まり "+Mathf.RoundToInt(Fill(b)*100)+"%）");
            GUILayout.Label("NEXT  "+PuzzleData.Names[Peek(b, 1)]);
            if(ai!=null){
                GUILayout.Label("⏱ あと "+Mathf.Max(0, b.Timer).ToString("F1")+" 秒  おじゃま待機 "+b.Pending.Count);
                GUILayout.Label("必殺技ゲージ "+b.Gauge+"%");
                GUI.enabled=human&&!finished&&b.Gauge>=100;
                if(GUILayout.Button((aiming&&human?"照準中：":"⚡")+PuzzleData.Specials[b.Hero]))Special(b, null);
                GUI.enabled=true;
            }
            GUILayout.EndArea();
        }
        void OnGUI(){
            if(player==null)return;
            float scale=Mathf.Min(Screen.width/1100f, Screen.height/800f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1100*scale)/2, (Screen.height-800*scale)/2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(10, 5, 1080, 115));
            GUILayout.Label("モンスター討伐パズル");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("ソロ")){
                mode=Mode.Solo;
                StartGame(true);
            }
            if(GUILayout.Button("⚔ ボス制圧戦（1人用）")){
                mode=Mode.Boss;
                StartGame(true);
            }
            if(GUILayout.Button("👙 AI対戦")){
                mode=Mode.Versus;
                StartGame(true);
            }
            if(GUILayout.Button("↺ はじめから"))StartGame(true);
            if(GUILayout.Button(sound?"🔊 音 ON":"🔇 音 OFF"))sound=!sound;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("キャラ", GUILayout.Width(45));
            if(GUILayout.Button("◀", GUILayout.Width(35))){
                hero=(hero+9)%10;
                player.Hero=hero;
            }
            GUILayout.Label(PuzzleData.Heroes[hero], GUILayout.Width(90));
            if(GUILayout.Button("▶", GUILayout.Width(35))){
                hero=(hero+1)%10;
                player.Hero=hero;
            }
            if(ai!=null){
                if(GUILayout.Button("AI ◀")){
                    enemy=(enemy+9)%10;
                    ai.Hero=enemy;
                }
                GUILayout.Label(PuzzleData.Heroes[enemy]);
                if(GUILayout.Button("AI ▶")){
                    enemy=(enemy+1)%10;
                    ai.Hero=enemy;
                }
                if(GUILayout.Button(new[]{
                    "弱い", "普通", "強い"
                }
                [difficulty]))difficulty=(difficulty+1)%3;
            }
            string shuffleLabel = player.ShuffleSettling
            ? "💥 揺らしてます…"
            : clock < player.ShuffleCooldownUntil
            ? "🌀 シャッフル（あと" + Mathf.CeilToInt(player.ShuffleCooldownUntil - clock) + "秒）"
            : "🌀 シャッフル";
            GUI.enabled = !finished && scene < 0 && !player.ShuffleSettling &&
            clock >= player.ShuffleCooldownUntil && player.Balls.Count > 0;
            if (GUILayout.Button(shuffleLabel)) Shuffle(player);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label("SCORE "+player.Score+"    BEST "+best+(mode==Mode.Boss?"    STAGE "+(bossStage+1)+" / "+PuzzleData.StageCount+"  ボス："+PuzzleData.BossNames[bossStage]+"  HP "+hp+" / "+MaxHp()+"  "+loop+"周目":""));
            GUILayout.EndArea();
            if(ai!=null){
                DrawBoard(player, new Rect(55, 120, 420, 568), true);
                DrawBoard(ai, new Rect(625, 120, 420, 568), false);
                Info(player, new Rect(55, 690, 420, 106), true);
                Info(ai, new Rect(625, 690, 420, 106), false);
            }
            else{
                DrawBoard(player, new Rect(30, 120, 460, 620), true);
                Info(player, new Rect(510, 120, 570, 75), true);
                GUILayout.BeginArea(new Rect(510, 200, 570, 540));
                if(mode==Mode.Boss){
                    int i=Mathf.Min(PuzzleData.BossImages[bossStage].Length-1, Mathf.FloorToInt((1-hp/(float)MaxHp())*PuzzleData.BossImages[bossStage].Length));
                    Image(GUILayoutUtility.GetRect(500, 180), PuzzleData.BossImages[bossStage][i]);
                }
                rulesScroll=GUILayout.BeginScrollView(rulesScroll);
                for(int i=0; i<11; i++)GUILayout.Label((i+1)+". "+PuzzleData.Names[i]+(i==10?"（最終形態） 合体で消滅 +500点":i==0?" 落下で登場":" 合体 "+((i+1)*(i+2)/2*10)+"点"));
                GUILayout.Label("通常玉が赤いラインより上に留まり続けると10秒のカウントダウンが進み、0になるとゲームオーバーです（ラインを下回ればその玉のカウントは解除されます。上端に触れただけ・一瞬ラインを越えただけでは負けません）。");
                if(mode==Mode.Boss){
                    GUILayout.Label("討伐済みCGギャラリー");
                    for(int i=0; i<PuzzleData.StageCount; i++)if(defeated[i]&&GUILayout.Button(PuzzleData.BossNames[i])){
                        galleryBoss=i;
                        galleryImage=0;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            if(finished&&GUI.Button(new Rect(490, 405, 120, 55), mode==Mode.Boss?"やり直す":"もう一度")){
                if(mode==Mode.Boss)StartGame(false);
                else StartGame(true);
            }
            if(clock<cutEnd&&cutKey!=null){
                Rect r=new Rect(cutSide==0?10:790, 650, 300, 100);
                Image(r, cutKey);
                GUI.Label(r, new[]{
                    "通常", "中破！", "大破！"
                }
                [cutStage]);
            }
            if(scene>=0&&scene<3){
                GUI.Box(new Rect(250, 130, 600, 590), "⚔ 討伐完了！");
                string key="assets/scenes/scene_char"+(hero+1).ToString("00")+"_"+PuzzleData.Keys[bossStage % PuzzleData.Keys.Length]+"_"+new[]{
                    "start", "mid", "after"
                }
                [scene]+".jpg";
                Image(new Rect(270, 170, 560, 470), key);
                GUI.Label(new Rect(270, 640, 560, 25), "Stage "+(bossStage+1)+" クリア！ "+(scene+1)+" / 3");
                if(scene>0&&GUI.Button(new Rect(270, 675, 120, 32), "前へ"))scene--;
                if(GUI.Button(new Rect(690, 675, 120, 32), scene==2?"閉じる":"次へ"))scene++;
            }
            if(galleryBoss>=0){
                GUI.Box(new Rect(200, 100, 700, 650), "討伐済み敗北CG："+PuzzleData.BossNames[galleryBoss]);
                Image(new Rect(220, 140, 660, 540), PuzzleData.BossImages[galleryBoss][galleryImage]);
                if(GUI.Button(new Rect(220, 700, 120, 35), "前へ"))galleryImage=Mathf.Max(0, galleryImage-1);
                if(GUI.Button(new Rect(360, 700, 120, 35), "次へ"))galleryImage=Mathf.Min(PuzzleData.BossImages[galleryBoss].Length-1, galleryImage+1);
                if(GUI.Button(new Rect(740, 700, 120, 35), "閉じる"))galleryBoss=-1;
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
