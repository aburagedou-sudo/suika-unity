# モンスター討伐パズル Unity 2D 移植試作（修正ラウンド）

新規 Unity 2D プロジェクトへ `Assets/MonsterPuzzle` をコピーし、空のシーンを開いて Play してください。`RuntimeInitializeOnLoadMethod` により `PuzzleGame` が自動起動します。

## 今回の修正
- ボス制圧戦を全16ステージに変更。`PuzzleData.StageCount = 16`。Stage16（セレーネ）撃破後にStage1へ戻り、周回数が増えてHP倍率が適用されます。
- ソロ／ボス制圧戦のシャッフルを盤面全体の物理的な揺さぶりに変更。全ピースに上向き・横向きの速度を加え、約0.72秒の盤面揺れを表示します。
- シャッフル中は新規ドロップと再シャッフルを禁止。全ピースが25 FixedUpdate連続で低速になった時点、または4秒のフォールバック期限で落ち着いたものとみなし、その後4秒間シャッフルを禁止します。
- シャッフル開始後3秒間、および収束後さらに最低1.8秒間はゲームオーバー判定を停止します。判定停止中は各通常玉の危険ライン滞在カウントをリセットします。
- 対戦のシャッフルは従来どおり、プレイヤー側の現在玉・NEXTの再抽選のままです。
- `PuzzleData.cs` と `PuzzleGame.cs` を通常の複数行形式に整形しました。機能分割は行わず、既存の責務・構造を維持しています。

## ファイル
- `Assets/MonsterPuzzle/Scripts/PuzzleData.cs`: 玉・キャラクター・ボス・必殺技データ
- `Assets/MonsterPuzzle/Scripts/PuzzleGame.cs`: ゲーム進行、Physics2D、描画、入力、AI、必殺技、シャッフル
- `Assets/MonsterPuzzle/Resources/OriginalRules.txt`: HTMLから抽出したルール資料
- `manifest.json`: 素材キー一覧
- `README.md`: 導入・変更・確認事項

## Unity側で必要な設定
- Active Input Handling: `Input Manager (Old)` または `Both`
- Physics2D Gravity: X=0、Y=-9.81
- Fixed Timestep: 0.0166667（推奨）
- 画像・音声を利用する場合は、`manifest.json` のキーに合わせて `Resources/assets/` 以下へ素材を配置してください。
- `.meta` は同梱していません。Unityでインポート時に生成してください。

## 未確認・仮置き事項
- Unity EditorおよびUnity参照アセンブリがこの実行環境にないため、参照アセンブリを使ったコンパイル確認は実施できていません。Unity上でのコンパイル・プレイ確認が必要です。
- Unity Physics2DはWeb版の独自物理と異なるため、反発・摩擦・落下軌道・合体タイミングには差が出る可能性があります。
- シャッフルの物理キックはHTMLのpx/s値をUnityのワールド単位へ換算した近似値です。収束速度や画面揺れの体感はUnity上で調整してください。
- 既存の簡易IMGUI、演出、素材プレースホルダー等は今回の依頼範囲外として変更していません。
