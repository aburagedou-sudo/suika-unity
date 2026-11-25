namespace MonsterPuzzle {
    public static class PuzzleData {
        public static readonly string[] Names = {
            "タマゴエビ", "ガブムシ", "アメーバ怪獣", "タコ怪獣", "トゲトゲ怪獣", "惑星怪獣", "渦巻き怪獣", "岩目怪獣", "溶岩怪獣", "太陽怪獣", "大口怪獣"
        }
        ;
        public static readonly string[] Colors = {
            "#f08300", "#58a832", "#2196d8", "#e0457b", "#3f6fd8", "#7d9bd6", "#8e44ad", "#7a9a3a", "#e0392b", "#f39c12", "#6c3fb5"
        }
        ;
        public static readonly string[] Keys = {
            "egg", "insect", "amoeba", "octo", "spiky", "planet", "swirl", "rockeye", "lava", "sun", "mouth"
        }
        ;
        public static readonly string[] Heroes = {
            "ロゼ", "カカオ", "スミレ", "ギン", "クロエ", "ミカン", "マロン", "アカネ", "ルリ", "ユカリ"
        }
        ;
        public static readonly string[] Specials = {
            "クレセントレイ", "バーストナックル", "斬撃", "ウィングレイ", "シールドバッシュ", "ミサイル一斉射撃", "フレイムアックス", "ダガースロー", "クリスタルプリズム", "グラビティウェーブ"
        }
        ;
        public static readonly string[] Descriptions = {
            "自分の盤面で通常玉のある高さからランダムに1ライン選び、その横一線に触れた通常玉を全部消す（即時発動）", "自分の盤面で通常玉のある位置からランダムに1点を選び、その半径内の通常玉を消し、周囲の玉の段階を入れ替えて弾き飛ばす（即時発動）", "自分の玉1個が2つに分かれて合体し1段進化する（最終段階なら消滅）", "自分の盤面のタップした列（縦一線）に触れた通常玉を全部1段進化させる（最終段階は消滅）", "自分の紫玉・濃紫玉を全部泡に変え、泡を全部消す", "自分の通常玉をランダムに5個消す", "自分の一番大きい通常玉を1個消す（同サイズは一番上のもの）", "自分の通常玉をランダムに1個消し、その周囲の通常玉を1段小さくする（最小段階は消える・即時発動）", "自分の一番小さい段階の通常玉を全部1段進化させる", "自分の盤面全体を揺らし、全通常玉に弾きを入れ、数個の玉同士で段階を入れ替える"
        }
        ;
        public static readonly string[] BossNames = {
            "ヒナタ", "サクラ", "カグラ", "ルミナ", "アヤメ", "ミドリ", "レオナ", "コハク", "アオイ", "ステラ", "シズク", "キキョウ", "フロスト", "モミジ", "ボルト", "セレーネ"
        }
        ;
        public static readonly int[] Radii = {
            16, 21, 27, 34, 42, 51, 61, 72, 83, 95, 105
        }
        ;
        public static readonly int[] BossHp = {
            350, 550, 800, 1100, 1500, 1900, 2300, 2700, 3100, 3500, 4000, 4500, 5000, 5500, 6000, 6500
        }
        ;
        public static readonly int[] Damage = {
            0, 3, 6, 10, 15, 21, 28, 36, 45, 56, 68
        }
        ;
        public static readonly string[][] BossImages = {
            new string[] {
                "assets/hinata.jpg", "assets/hinata_n1105.jpg", "assets/hinata_n1106.jpg", "assets/hinata_m1107.jpg"
            }
            , new string[] {
                "assets/sakura.jpg", "assets/sakura_mid.jpg", "assets/sakura_lose.jpg", "assets/sakura_no1111.jpg"
            }
            , new string[] {
                "assets/kagura.jpg", "assets/kagura_n1110.jpg", "assets/kagura_m1113.jpg"
            }
            , new string[] {
                "assets/lumina.jpg", "assets/lumina_m1108.jpg", "assets/lumina_m1109.jpg"
            }
            , new string[] {
                "assets/ayame_n1104.jpg", "assets/ayame_m1105.jpg", "assets/ayame_no1105.jpg", "assets/ayame_no1107.jpg", "assets/ayame_m1117.jpg"
            }
            , new string[] {
                "assets/midori.jpg", "assets/midori_cap.jpg", "assets/midori_m1106.jpg", "assets/midori_n1107.jpg", "assets/midori_no1112.jpg", "assets/midori_n1114.jpg"
            }
            , new string[] {
                "assets/leona_lose.jpg", "assets/leona_n1111.jpg", "assets/leona_m1116.jpg"
            }
            , new string[] {
                "assets/kohaku.jpg", "assets/kohaku_cap.jpg", "assets/kohaku_no1110.jpg"
            }
            , new string[] {
                "assets/aoi.jpg", "assets/aoi_no1113.jpg", "assets/aoi_no1115.jpg"
            }
            , new string[] {
                "assets/stella.jpg", "assets/stella_m1111.jpg", "assets/stella_n1115.jpg", "assets/stella_no1117.jpg"
            }
            , new string[] {
                "assets/shizuku.jpg", "assets/shizuku_m1110.jpg", "assets/shizuku_n1116.jpg"
            }
            , new string[] {
                "assets/kikyo_no1109.jpg", "assets/kikyo_n1109.jpg", "assets/kikyo_no1114.jpg", "assets/kikyo_no1116.jpg"
            }
            , new string[] {
                "assets/frost_no1108.jpg", "assets/frost_n1113.jpg"
            }
            , new string[] {
                "assets/momiji_n1108.jpg", "assets/momiji_m1112.jpg"
            }
            , new string[] {
                "assets/bolt_m1114.jpg", "assets/bolt_m1115.jpg"
            }
            , new string[] {
                "assets/selene_n1112.jpg"
            }
        }
        ;
        public const int StageCount = 16;
    }
}
