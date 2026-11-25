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
            "ロゼ", "カカオ", "スミレ", "ギン", "クロエ", "ミカン", "マロン", "アカネ", "ルリ", "ユカリ"
        }
        ;
        public static readonly int[] Radii = {
            16, 21, 27, 34, 42, 51, 61, 72, 83, 95, 105
        }
        ;
        public static readonly int[] BossHp = {
            350, 550, 800, 1100, 1500, 1900, 2300, 2700, 3100, 3500
        }
        ;
        public static readonly int[] Damage = {
            0, 3, 6, 10, 15, 21, 28, 36, 45, 56, 68
        }
        ;
        public static readonly string[][] BossImages = {
            new string[] {
                "assets/scenes/scene_char01_egg_start.jpg", "assets/scenes/scene_char01_egg_mid.jpg", "assets/scenes/scene_char01_egg_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char02_insect_start.jpg", "assets/scenes/scene_char02_insect_mid.jpg", "assets/scenes/scene_char02_insect_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char03_amoeba_start.jpg", "assets/scenes/scene_char03_amoeba_mid.jpg", "assets/scenes/scene_char03_amoeba_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char04_octo_start.jpg", "assets/scenes/scene_char04_octo_mid.jpg", "assets/scenes/scene_char04_octo_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char05_spiky_start.jpg", "assets/scenes/scene_char05_spiky_mid.jpg", "assets/scenes/scene_char05_spiky_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char06_planet_start.jpg", "assets/scenes/scene_char06_planet_mid.jpg", "assets/scenes/scene_char06_planet_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char07_swirl_start.jpg", "assets/scenes/scene_char07_swirl_mid.jpg", "assets/scenes/scene_char07_swirl_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char08_rockeye_start.jpg", "assets/scenes/scene_char08_rockeye_mid.jpg", "assets/scenes/scene_char08_rockeye_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char09_lava_start.jpg", "assets/scenes/scene_char09_lava_mid.jpg", "assets/scenes/scene_char09_lava_after.jpg"
            }            ,             new string[] {
                "assets/scenes/scene_char10_sun_start.jpg", "assets/scenes/scene_char10_sun_mid.jpg", "assets/scenes/scene_char10_sun_after.jpg"
            }
        }
        ;
        public const int StageCount = 10;
    }
}
