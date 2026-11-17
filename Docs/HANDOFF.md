# Day 113 引継ぎ
- 状態：Project作成済み（2026-11-15、Tools/workspace.py new-day 113、空Project＝Packages/ProjectSettingsのみStarterから）。SPEC確定・CHECKLIST作品固有項目を追記。実装計画の承認待ち4項目は回答済み（PLAN.md「回答」）
- 編集担当：Claude
- 変更：SPEC.md 全欄記入、CHECKLIST.md 作品固有21項目追記、Project/ 新規
- Unity操作手段：Unity CLI 1.0.0＋com.unity.pipeline 1.0.0 で接続確認済み（unity-ready 113 → ready、ポート7800）。予備でMCP for Unity v10.0.0も導入済み
- コンパイル：パッケージ追加後のコンパイル完了まで確認（自作C#はまだない）
- PC試遊：未確認
- スマホ実機：未確認
- Webビルド：未実施
- 公開URL/反映照合：未実施
- 2026-11-15 クラウドセッションから実施：資料確認と4項目の回答取得のみ。クラウドのシェルはLinux VMでunityコマンドが無く、MacのEditorに接続不可（Knowledge記載どおり）
- 次の1手：Codeタブ・Local・TitanUnity100で unity status → play 113 → unity-ready 113 → パッケージ追加・固定 → 段階1着手

## 2026-11-15 ルール一本化にともなう変更（クラウドセッション・実装なし）
- Unityの共通規則と知見（Rules/・Knowledge/）の実体を GitHub-game/docs/unity/ へ移設。TitanUnity100側はリンクなので、読む手順・パスは今までどおり（day113/Projectからの読み込み7本をリンク越しに確認済み）
- Rules/CORE.md に「GitHub-game共通ルールとの接続」を追記（Slack完了報告は毎回・証跡・20MB・接続ルールの場所）
- Knowledge/CLAUDE_CODE_APP_UNITY_CLI.md の先頭に「unameがLinuxなら止まってLocalへ誘導」を追記
- SPEC.md・CHECKLIST.md に、HTML版の制作中に出た指示で抜けていた項目（スクロール禁止・文字少なめ・ひじ膝・虫眼鏡・壁抜けなし）を追記
- 未決（タイタン判断待ち）：公開先リポ名（PLANの想定 `unity-day113-iromaze-gahaku` は GitHub-game の「フォルダ名＝リポ名＝URL」規則と合わない）、GitHub Pagesのトークン（2026-11-14時点で無効、HTML版はアーティファクトで代用）、3Dアセット込みWebGLの20MB上限
- 次の1手（変更なし）：Codeタブ・Local・TitanUnity100 で unity status → play 113 → unity-ready 113 → パッケージ追加・固定 → 段階1

## 2026-11-15 下ごしらえ（クラウドセッション・Unity未起動）
- manifest.json に URP 17.3.0 / Timeline 1.8.13 / Cinemachine 3.1.2 / uielements 1.0.0 を追加（根拠はPLAN.md「下ごしらえ」）。lockfile未更新・コンパイル未確認
- 一致テストの正解データ `Golden/` を作成：mixRGB 4927通り、3ステージのお手本・bestRecipe、grade 18ケース（各ステージ6種、0〜90点で50点の境界をまたぐ）。HTML版coreをChrome 153で実行して生成。保存データをNodeで読み戻し、ステージ1・2の12ケースで点数と内部値の一致を確認済み
- 変更前のファイル：GitHub-game/backups/2026-11-15-ai-rules/ssd/day113/
- 次の1手：Codeタブ・Local・TitanUnity100 で `unity status` → `python3 Tools/workspace.py play 113` → `unity-ready 113` → lockfile更新とConsoleエラー0を確認 → URPのPipeline Asset割り当て → 段階1

## 2026-11-15 段階1 着手（チャットからUnity MCP経由・Editor ready）
- URP：Assets/Settings/D113_URP.asset を作成し Graphics/全Quality に割り当て。シーン Assets/Scenes/Atelier.unity（Build Settings登録済み）
- 混色：Scripts/MixCore.cs。Golden/mix.json 4927通りと一致（最大差 2.8e-14）
- 筆：Scripts/PaintCanvas.cs＋Resources/Shaders（Stamp/Bleed/Lit）。1024px、毛の筋・かすれ・厚塗り（高さ→法線→スペキュラ）・ぬれ色のにじみ、1筆もどす12段
- 画面：Scripts/Atelier.cs（UI Toolkitのみ）。チューブ5色・−・12滴・ストック6・筆4段（初期=太）・あらう・まっさら2度押し・見せる(4000未満不可)・お手本拡大・ルーペ（タッチのみ）。縦持ち＝キャンバス横幅いっぱい、横長＝左キャンバス
- 布張りキャンバス見た目（側面の厚み・鋲・影・麻の繊維むら）
- 効果音：Scripts/Sfx.cs（合成PCM。筆音は速さで音量とピッチ、絞る/ぽとっ/洗う/タップ）
- スクショ：Shots/stage1_v2.png。Consoleエラー0
- 未：採点（段階2）、💡ヒント（段階2）、パレットのとろっと表現、振動、統計値(paintedPx)のHTML版との較正（段階2で一致テストと合わせて確認）
- 追加注文（タイタン 16:55）：書体＝メガドライブ時代っぽいドット文字（既存ゲームの文字は写さずオリジナル）、BGM＝クラシック調の上品な曲（段階5）
- 次の1手：タイタンにスクショ確認 → OKなら段階2（採点移植＋Golden一致テスト）

## 2026-11-15 段階2 採点（チャットからUnity MCP経由）
- 採点：Scripts/Grader.cs（getRef/bestRecipe/grade 移植）・Judges.cs（講評と評語）・Stages.cs（3ステージの文言）。お手本は Resources/Refs/stage{1,2,3}.json/.png（Golden/のコピー）
- 一致テスト：Editor/GoldenCheck.cs（メニュー Day113/一致テスト）。grade 18ケース＋bestRecipe 13件＝31/31一致、f最大差 2.2e-16
- 絵と採点の連動：「見せる」でキャンバスを256へ縮めて読み出し（PaintCanvas.Sync＋Shaders/D113Sync）、そのまま grade に渡す。丁寧に描いた静物＝74点、黒まじりのちょこちょこ塗り＝10点を確認
- 画面：結果パネル（絵・★・総合カウントアップ・評語・3人の点数と講評・解放/あと◯点・見なおす/ステージ/もう一枚）、ステージ選択（3枚＋つづく、50点で解放）、ヒント（推奨配合を滴で表示）、横ロック（ステージ2のみ）
- 保存：PlayerPrefs d113_best_1〜3（ベスト点・解放）。テストで入った値は消去済み
- スクショ：Shots/stage2_select.png, stage2_result_good.png, stage2_result_bad.png, stage2_hint.png。Consoleエラー0
- 未：音のON/OFFと保存（ボタン未作成）、塗り量(paintedPx)はHTML版と単位をそろえた見積もり（4000/3000のしきい値は動作確認のみ）
- 追加注文（タイタン 17:04）：最後にスタート画面を作り、そこにだけBlender製「苦労人の画家」（360度カメラ・考える/創作に燃えるポーズ）を出す
- 追加注文（タイタン 17:07）：ここからCodexの画像作成を使いたい → 現状不可（VMからMacのCodexは見えず、OpenAIへの通信も遮断）。CodexをローカルMCPとして登録する手順を用意する
- 次の1手：タイタン確認 → 段階3（Blender素材とアトリエ）

## 2026-11-15 Codex画像作成をチャットから使う準備
- Tools/add_codex_mcp.command を作成：Codex公式の `codex mcp-server` を Claude アプリのローカルMCP（名前 codex）に登録。登録前の設定は Reports/backups/ に退避
- タイタンがターミナルで実行 → Claudeアプリ再起動 → 「できた」で、チャットから day113 の参考画像を1枚作って動作確認する

## 2026-11-15 17:30 段階3 着手（Codex画像→Blender待ち）
- Codexの窓口：公式 mcp-server は廃止のため、自作 ~/.local/titan-mcp/codex_bridge.py を「codex」で登録（codex_run/status/stop/raw）。チャットから codex exec が通ることを確認
- 参考画像：Refs/concept_atelier.png（1536x1024、Codex gpt-image、42k tokens・75秒）。窓の夕方斜光・イーゼル・小机にパレットとチューブ5色・筆立て・木の床。アトリエのBlender制作の見本にする
- Codex側の注意：起動時に 127.0.0.1:8080/mcp への接続エラーが出る（未起動のMCP登録が残っている）／config.toml の features.rmcp_client は無視される。動作に影響なし
- 状態：Unity Editorはプレイ中のまま。Blenderは未起動（MCP接続不可）
- 次の1手：タイタンがBlenderを起動（MCPアドオンON）→ 参考画像を見本にアトリエ（部屋・窓・イーゼル・小机・パレット・チューブ・筆立て）をローポリで作る → FBXでUnityへ → Atelierシーンの背景に配置

## 2026-11-15 18:00 段階3 アトリエ3D（チャットからBlender MCP＋Unity Pipeline）
- Blender 5.2.3（MCP for Blender、127.0.0.1:9876）でアトリエをローポリ作成：床板・漆喰壁3面・天井と梁・窓（枠/夕空の発光面/カーテン）・イーゼル・キャンバス・小机・パレット・チューブ5色・筆立て・額2枚・本棚・壺・植木など105点。原本 Blender/atelier.blend
- 注意：Blender 5.2は日本語UIだとノード名も日本語（「プリンシプルBSDF」）。ノードは名前でなく bl_idname で探す
- 書き出し：Project/Assets/Models/D113_Atelier.fbx（ImportStandard、カメラ/ライト/アニメ取り込みなし、URP/Litに自動変換）
- Atelierシーン：AtelierRoom を配置、Main Camera (-0.2,1.55,3.4) rot(5,180,0) FOV62、D113_Sun（暖色の平行光、窓から差し込む影）、D113_Fill（点光源）、環境光フラット。保存済み
- Atelier.cs 88行：root背景を不透明→半透明（.38）にして後ろの3D部屋を透かす
- スクショ：Shots/stage3_room_cam.png（部屋のみ）、stage3_atelier.png（UI込み）、stage3_select_room.png。Consoleエラー0
- 未：UIのキャンバスと3Dのキャンバスの位置合わせ（今は3Dのイーゼルが下にのぞく程度）、ギャラリー（段階4）、人物7種（段階3の残り：共通骨格＋衣装違い）、スタート画面の「苦労人の画家」
- 次の1手：人物の共通骨格（先生3人から）をBlenderで作る

## 2026-11-16 17:05 段階3 人物（先生3人）＋段階4 ギャラリー（Blenderまで）
- Blender自動接続：~/Library/Application Support/Blender/5.2/scripts/startup/titan_mcp_autoconnect.py を設置（タイタンがターミナルで実行）。起動2秒後に自動でMCPサーバー開始 → 開き直しで自動接続を確認済み
- 先生3人：Blender/teachers.blend → Project/Assets/Models/D113_Teachers.fbx（未インポート）
  - 共通骨格（hips/spine/chest/neck/head、upperarm/forearm/hand、thigh/shin/foot の左右）、部位は骨に親子付け（ひじ・ひざがきれいに曲がる）
  - Teacher_Elder（白ひげ・茶のスーツ・後ろ手）、Teacher_Critic（深緑ドレス・まとめ髪・腕組み）、Teacher_Loupe（ベレー帽・眼鏡・前かがみで虫眼鏡を覗く）
  - ポーズはアクション Elder_Pose / Critic_Pose / Loupe_Peer として書き出し
  - 骨の回転の向き（覚え書き）：上向きの骨は X+ で前へ倒れる。下向きの骨（腕・脚）は X+ で後ろ、X− で前。ひじ・ひざを曲げるのは前腕 X−、すね X+
- ギャラリー：見本 Refs/concept_gallery.png（Codex）→ Blender/gallery.blend → Project/Assets/Models/D113_Gallery.fbx（未インポート）
  - 赤い壁・白い巾木と回り縁・木の床・天井の間接照明・右奥のアーチと胸像・金の額（中央の PlayerPainting 面にプレイヤーの絵を貼る想定）・ネームプレート・スポットライト器具・金のポールと赤いロープ・左右の額4枚
- 未：Unityへのインポートと配置（Unityが閉じていたため）。Codexから workspace.py play 113 を実行したが、Codexの安全な実行環境からは Unity.app を起動できなかった（kLSNoExecutableErr）
- 次の1手：Unityを開く → 2つのFBXをインポート → Gallery シーン作成（先生3人を配置、PlayerPainting に Canvas.Display を貼る）→ 審査の流れ（先生が1人ずつ歩いて講評）

## 2026-11-16 17:15 ギャラリーシーンの自動組み立てを仕込み
- Project/Assets/Editor/D113GallerySetup.cs を追加：Unityが読み込むと一度だけ自動実行（Temp/d113_gallery_setup_done で二重実行を防ぐ）。メニュー Day113/ギャラリーを組み直す でも実行可
  - FBX2つの取り込み設定 → Assets/Scenes/Gallery.unity を新規作成（展示室・先生3人をポーズ付きで配置・カメラ・スポットライト・暖色の点光源）→ Build Settingsに追加 → Shots/stage4_gallery.png を撮影 → 元のシーンを開き直す
  - 失敗時は Temp/d113_gallery_setup_done_error に内容を残す
- 未実行：Unityが裏にあると取り込み（Refresh）が走らず、画面ロック中で操作もできなかった
- 次の1手：Unityのウィンドウを一度前に出す（自動で走る）→ stage4_gallery.png を確認 → 審査の流れ（先生が1人ずつ歩いて講評）

## 2026-11-16 18:01 Galleryシーン完成（メニュー Day113/ギャラリーを組み直す）
- Assets/Scenes/Gallery.unity を作成・Build Settings追加。展示室＋先生3人（ポーズ付き）＋スポット（4.5）＋天井の暖色点光源（0.9）×3、カメラ (0,1.55,3.6)
- スクショ：Shots/stage4_gallery.png（光を弱めて撮り直し済み）
- 知見：チャットの途中でUnityを起動すると、Unity Pipelineの操作ツールがそのチャットに出てこない。代わりに「Editorスクリプトを置く → コンピューター操作でメニュー Assets/Refresh → 自作メニューを押す」で裏から動かせた（ウィンドウを前に出さなくてよい）
- 次の1手：PlayerPainting に描いた絵（Canvas.Display）を貼る → 審査の流れ（先生が1人ずつ絵の前へ歩く → 虫眼鏡で覗く → 講評）→ 展示会

## 2026-11-16 18:20 タイタン指摘の反映（イーゼル・黒猫・人物の作り直し）
- イーゼル：左右の脚を「上がマストに寄り、下が広がる」A字に修正（EaselLegL/R ±13°、横木を1.25倍）
- 黒猫：アトリエのイーゼル左の床に座らせた（Cat_* 18点、黄色い目）
- 先生3人を「Blender＋画像の融合」で作り直し：
  - 体：骨に沿ったスキンモディファイア＋細分化でなめらかな1枚の体にし、自動ウェイトで骨に結合（ひじ・ひざが自然に曲がる）。首は別パーツ
  - 服：Codexで全身の正面図（日本人・Aポーズ）Refs/body_*.png を作り、正面から投影してUVに貼った。腕はAポーズの位置に合わせて投影をずらす
  - 顔：Codexで日本人のあっさりした顔 Refs/face_*.png を作り、頭の正面の面だけ顔の絵に差し替え（UVは1枚に統合）
  - 評論家：腰から広がるフレアスカートを作り直し。髪・まとめ髪・ベレー帽は単色
- Unity：Assets/Textures/Teachers に絵を置き、D113GallerySetup の RemapTeacherMaterials で URP/Lit の素材に差し替え（FBX埋め込みの絵はUnityで貼られなかったため）
- 新メニュー：Day113/アトリエを撮る（Shots/stage3_room_cam.png）
- スクショ：Shots/stage4_gallery.png、stage3_room_cam.png、stage34_pair.png
- 未：足先・ひじの一部が白くなる（投影の端）、顔の横と髪のつなぎ目。審査の流れは未着手

## 2026-11-16 19:00 段階4 審査の流れ（動作確認済み）
- 流れ：「見せる」→ Galleryを追加読み込み（アトリエの部屋と照明は審査中だけ隠す）→ 額に描いた絵を貼る → 先生が1人ずつ壁ぎわから絵の前へ歩く → 絵の方を向いて覗く（カメラが寄る）→ 振り向いて講評（文字送り）と点数（カウントアップ）→ 元の場所へ戻る → 総合点と評語 → 展示室を閉じて、これまでの結果パネルを出す。タップで早送り
- 担当：藍原先生（色彩）＝老大家、方円先生（形）＝評論家、ふわり先生（感性）＝虫眼鏡の先生
- ファイル：Scripts/JudgeShow.cs（演出本体）、Scripts/JudgeRig.cs（先生とクリップの参照）、Atelier.cs の Submit を差し替え、Editor/D113GallerySetup.cs がJudgeRigを組み込む
- テスト：メニュー Day113/審査テスト → テスト用の絵を自動で描いて審査を流し、Shots/judge_*.png を撮る（Scripts/JudgeTestHook.cs、エディタ専用）。まとめ画像 Shots/judge_sheet.png
- Blender：歩き（*_Walk）と立ち（*_Stand）のアクションを追加。全アクションを書き出し
- 知見（重要）
  - FBXのクリップは「先生3人の親（Teachers）」基準のパスで、各先生のルートの回転・位置も含む → SampleAnimation は親に当て、直後に3人のルートの位置と向きを戻す（JudgeShow.Samp）
  - 先生の向きを変えるときは「元の傾き（-90°X）」に左右の回転を掛ける（LookRotation(dir) * baseRot）。上書きすると床に倒れる
  - 正面投影の絵は裏側にも映る → 後ろ向きの面は、後頭部＝髪の色、背中＝脇の布へ貼り直した
  - 額の箱はUVの向きが合わないので、実行時に正しい向きの板（PlayerArt）を重ねて絵を貼る
- 未：総合点の場面が暗く遠い、虫眼鏡先生の覗く動作をもっと大きく、展示会（客・行列・富豪・怪盗）は次

## 2026-11-16 19:40 展示会（コードとモデルまで。Unityでの確認は未）
- 道具化：Blender/people_build.py（人物づくり一式：骨・なめらかな体・スカート/マント・正面投影＋裏側処理＋白はみ出し補正・アクション・FBX書き出し）。exec で読み込んで使う。画素の上下（Blenderは下から）に注意
- Codex：Refs/body_visitor_m / visitor_f / rich / thief / staff.png（まとめ Refs/crowd_sheet.png）
- Blender/crowd.blend → Assets/Models/D113_Crowd.fbx（5人。体はリグの子＝複製しやすい。各人 Walk/Stand/Peer/Clap/Surprise/Point/Bow、怪盗はシルクハットとマント）
- 先生にも *_Surprise を追加して D113_Teachers.fbx を書き出し直し
- Unityコード：Scripts/CrowdKit.cs（ひな形とクリップ、Actor＝包みの下にリグを複製してクリップを当てる）、Scripts/Exhibit.cs（SPECどおり：来場 round(総合×0.3)+4、ペース .35+総合/100×1.25、扉→行列＋最後尾札→鑑賞枠5→反応♥/？/汗（担当観点の点数/100）→退場、HUD、70以上で開幕9秒後に大富豪→スタッフが価格50/300/1000/5000万円、80以上で大富豪のあと怪盗：停電→口上→全員驚く→逃走→復旧、閉幕バナー（30/20/12人））
- JudgeShow：総合のあとに展示会を流す。総合の場面はカメラを寄せ、虫眼鏡先生はぐっと絵に近づいてカメラも寄る。Banner/Bubble は JudgeShowUI へ
- Editor/D113GallerySetup.cs：群衆の取り込み・素材差し替え・CrowdKit組み込み・先生の驚きクリップ割り当てを追加
- 未実行：画面ロックでUnityのRefreshができず、コンパイル・組み直し・テスト未。次の1手：Assets/Refresh → Day113/ギャラリーを組み直す → Day113/審査テスト（Shots/exhibit_*.png も撮れる）→ 直す

## 2026-11-16 20:15 展示会 動作確認（高得点テストで最後まで通過）
- メニュー追加：Day113/審査テスト（高得点：大富豪と怪盗）＝総合90点で流す。どちらのテストも必ずAtelierを開いてから再生する
- 確認できた流れ：審査 → 総合90点「大行列まちがいなし！」→ 展示会スタート → 来場31人が扉から入場・行列（最後尾札）・鑑賞・♥反応・退場 → 開幕9秒後に大富豪「これは…買えるのかね？」→ スタッフが値段 → 停電して怪盗の口上、全員が驚く → 逃走・復旧 → 閉幕「来場31人 ♥31 大盛況！」→ 結果パネル。Shots/exhibit_*.png、まとめ exhibit_sheet.png
- 直したこと
  - Blenderは使っていない動き（アクション）を保存時に捨てる → people_build.py の key_action で use_fake_user=True。先生の歩き・立ちを作り直し
  - UnityのFBX取り込みで体（SkinnedMesh）が一番上の階層に出る → 組み立て時に各人の骨組みの中へ戻す（そうしないと複製で体が付いてこない）
  - 複製した体は表示範囲が元の位置のまま → updateWhenOffscreen=true
- 未・気になる点（タイタン確認待ち）：人が多いと行列と鑑賞の人が重なって見える（めり込みチェック未）、大富豪が人混みで見えにくい、怪盗の場面が暗い、BGM・効果音の切り替えは段階5

## 2026-11-16 20:45 テストプレイの指摘を反映
- 画面：Scripts/FitBox.cs を追加。重ね表示（ステージ選択・ヒント・結果）は中身を画面に収まるまで縮める。縦持ちは道具の列を幅に合わせて縮め、横長は右側（名前＋道具）を枠に収める。スクロールなし・文字の重なりなしを確認（メニュー Day113/画面チェック（スマホ縦・横）→ Shots/fit_*.png、まとめ fit_sheet.png）
- 審査：HTML版と同じく、柵（ロープ）の端を回り込んで絵の前に立ち、全員が虫眼鏡で覗く（*_Inspect アクション、虫眼鏡は覗くときだけ表示）。カメラは横から先生と絵を両方入れる
- 先生の見た目：髪をかぶせて後頭部のにじみをなくした、首の位置ずれを修正（虫眼鏡先生の肩の筒）、ベレー帽を頭にのる形に、顔の絵の目鼻を濃く（Refs/face_*_hc.png）、背中の白い抜けを服の色に
- 靴：足先を全身画像の靴の色で塗る（people_build.py の paint_shoes）。先生・来場者とも
- テスト後は Edit/Clear All PlayerPrefs でベスト点と解放をもどすこと

## 2026-11-17 19:55 審査カメラをWeb版の動きに／点数札「ボン」を追加
- 決定：しゃべる声は「ポポポ」音（段階5で、先生ごとに音の高さを変える）
- 参考：タイタンが録画したWeb版の審査（カメラの動きが気に入っている）→ カット表は CAMERA_REF.md
- カメラ：Scripts/JudgeCam.cs を追加（目標位置と注視点へ指数でなめらかに寄る＝Web版の shot() と同じ。follow で歩く先生を追う。縦持ちFOV60／横44）
  - 流れ：引きの斜め → 絵の正面へ寄る（タイトル）→ 歩く先生を後ろから追う → 虫眼鏡は肩ごしアップ → 振り向いて正面バストアップ → 札で少し引く → 戻る先生を追う
  - Unityは左手系なので、Web版の x はそのままでは左右が逆。カメラは先生が来た側（2人目だけ待っている先生をよけて反対側）
- 札：Scripts/ScoreCard.cs を追加（実行時に棒＋赤ふち白札＋数字を作る。右腕 upperarm.R/forearm.R を真上へ0.25秒で行き過ぎて戻し、札は25°で出て揺れて止まる）。点数の色は70以上赤／40以上黒／それ未満青
  - 同時に右下の点数がスタンプのようにポン（0→1.25→1倍）＋カウントアップ。「ボン」音は今は仮（Plop＋Chime）
  - 「…じ〜っ……」を覗いている間に出すようにした（Web版と同じ）。点数は札のときまで隠す
- 直したこと：カウントアップがコマ落ちで一瞬行き過ぎる（94点など）→上限をつけた。審査中にAudioListenerが無い警告 → 審査カメラに付ける
- 新しいスクショ：Shots/judge_title / judge_N_walk / judge_N_card.png
- 元のJudgeShowは Backup/JudgeShow_20261117.cs
- 次の1手：Webと見比べてタイタン確認 → 気になってる所（行列と鑑賞の重なり、大富豪が見えにくい、怪盗が暗い）→ 段階5（BGM4曲・ポポポ声・ドット文字・音ON/OFF）→ WebGL公開

## 2026-11-17 20:15 iPhone向け画面チェック・WebGLビルド（公開はリポジトリ作成待ち）
- 審査テスト3回でカメラと札を確認（Shots/judge_title, judge_N_walk/inspect/comment/card）。札の数字と右下の点数が一致
- 画面チェック（スマホ縦・横）：ステージ選択・アトリエ・結果とも、はみ出し・スクロール・文字の重なりなし（Shots/fit_*.png）
- メニュー Day113/WebGLビルド（Editor/D113WebBuild.cs）：圧縮なし・productName「113 いろまぜ画伯」・ビルドIDを Resources/buildid.txt に書いて左下に薄く表示（Scripts/BuildStamp.cs）
- ビルド d113-20261117-2002：成功・エラー0・7分15秒、Builds/Web（17ファイル 69.9MB、Web.wasm 51.6MB＝GitHubの50MB警告は出るがpush可）。check-web OK
- index.html を手直し：スマホは全画面・ピンチ/長押しメニュー無効・解像度の上限2倍、PCもウィンドウいっぱい（※Unityで再ビルドすると元に戻る → 次はWebGLTemplatesにする）
- GitHub：このチャットの環境からは新しいリポジトリを作れない。タイタンに `unity-day113-iromaze-gahaku` の作成を依頼中。中身はクラウド側でコミット済み（main=ソース、gh-pages=ビルド）
- Builds/_to_delete/_upload_day113.tgz は受け渡し用の一時ファイル（消してOK）
- テスト後に Edit/Clear All PlayerPrefs 済み
