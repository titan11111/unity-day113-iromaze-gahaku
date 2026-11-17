# Day113 実装計画（2026-11-15 提出・4項目回答済み）

> 再開するときは HANDOFF.md → このファイル → SPEC.md → CHECKLIST.md の順に読む。Unityへの接続手順は ../../Knowledge/CLAUDE_CODE_APP_UNITY_CLI.md。

## 最初にやること
1. `unity status`（または `python3 Tools/workspace.py unity-ready 113`）で ready になることを確認する。Editorが閉じていれば `python3 Tools/workspace.py play 113` で開く。
2. 下の「承認待ちの4項目」についてタイタンに確認し、答えをもらってから段階1に入る。

## 承認待ちの4項目
1. UI Toolkitを使うか。CORE.mdには「StarterはuGUI」とあるが、このProjectは空から作ったので、最初からUI Toolkitだけで統一してuGUIと混ぜない、という案。
2. 追加パッケージ（URP・Timeline・Cinemachine・UI Toolkitのモジュール）を入れて、manifestとlockfileで固定してよいか。
3. BGMの曲数。依頼は3曲（アトリエ/展示会/怪盗）だが、HTML版には審査のBGMもある。4曲にするか、審査はアトリエの曲を流用するか。
4. 保存の範囲。(a) HTML版と同じ（ステージ別ベスト・解放状態・音のON/OFF）にするか、(b) 描きかけの絵の自動保存と再開を足すか（メニューは足さない）。

## 回答（2026-11-15 タイタン）
1. UI Toolkitで統一（uGUIは混ぜない）
2. URP・Timeline・Cinemachine・UI Toolkitモジュールを全部入れ、manifestとlockfileで固定
3. BGM曲数：タイタンは「どちらでも」→ ClaudeがHTML版どおり4曲（アトリエ/審査/展示会/怪盗）に決定
4. 保存：(a) HTML版と同じ（ステージ別ベスト・解放状態・音ON/OFF）。描きかけの自動保存はしない

## HTML版の構成（読み済み）
script4本：①core（mixRGB・rgb2lab・お手本3つ・Painter・grade。末尾に module.exports があり、nodeで答え合わせに使える）②SND（WebAudioの合成BGM4曲＋効果音＋筆音）③G3/World/Show（ギャラリー・人物・審査・展示会・VIP・怪盗）④UI（チューブ・ストック・筆サイズ・横ロック・1筆もどす・ルーペ・ヒント・講評・解放・localStorage）。

## 方針（2倍の作り方）
- 点数一致：筆は1024以上で描き、採点のときだけ256に縮める。筆の統計（塗り量・筆の長さ）は256座標系にそろえ、HTMLのしきい値（提出に必要な塗り量4000、まっさらの確認3000、avgLen 30〜120 など）を守る。テストは同じ256入力をJS版とC#版に入れて比べる。ステージ3の写真は、ブラウザで読み込んだ画素を共通データとして固定して両方で使う。
- 筆：GPUで処理する（色・高さ・ぬれ具合のRenderTextureにスタンプする）。厚塗りはURPシェーダーで高さ→法線→スペキュラ。にじみはぬれ具合の画像から作る。速度は全端末で反映し、筆圧は取れる端末だけで反映する。
- 振動：jslibで navigator.vibrate を使う（Android Chromeのみ。iOSは非対応と記録する）。
- 音：WebGLでは OnAudioFilterRead が使えないので、HTML版のシンセを事前にWAVへ書き出して使う。筆音はループ音の音量とピッチを筆の速さに連動させる。
- Blender MCP：「あたたかい油絵調のローポリ」で統一。人物は共通の骨格1つを衣装違いで7種にする（先生3・スタッフ・大富豪・怪盗・来場者）。アニメは歩く/見る/驚く/拍手と、HTMLにある話す/お辞儀/忍び足/札を掲げる/後ろで見る。作るたびにスクショで確認する。
- 演出：Timeline＋Cinemachine、ポストプロセス（ブルーム・色調整・周辺減光）、スポットライト＋ベイク。
- 進め方：段階1 お絵描きの核 → 2 採点移植・3ステージ・保存 → 3 Blender素材とアトリエ → 4 展示会・イベント・演出 → 5 音・仕上げ・性能・公開。各段階の終わりに、Claudeが自分で撮ったスクショを見せて止まる。変更のたびにコンパイルとConsoleエラー0を確認する。
- 完了：CHECKLISTを証拠つきで全部埋める。HTML版と並べたスクショ比較を出す。WebGLビルド → GitHub Pages（titan11111/unity-day113-iromaze-gahaku を想定。公開前に設定を確認）→ プレイURLつきでSlack #general へ報告。HANDOFFを更新する。

## 話し方
タイタンへの返答はギャル風。最初に今日の日付を確認し、パターン「B ゲームクリエイター」で始める。

## 下ごしらえ（2026-11-15 クラウドセッションで実施・Unityは未起動）
1. パッケージを manifest.json に追加済み（回答2の実行）。バージョンの根拠：
   - URP 17.3.0 … Editor 6000.3.26f1 に同梱（BuiltInPackages）
   - Timeline 1.8.13 … Editor の推奨バージョン（PackageManager/Editor/manifest.json）
   - Cinemachine 3.1.2 … day105 で使用実績あり（Unity 6向けの新しいAPI `Unity.Cinemachine`。Editor推奨の2.10.7は旧API）
   - UI Toolkit … `com.unity.modules.uielements` 1.0.0 を明示
   - **lockfile はまだ古いまま**。Editorを開くと自動で更新される → 更新後の packages-lock.json に4つが入ったことと、Consoleエラー0を確認する
   - URP は入れただけ。Pipeline Asset の作成と Graphics / Quality への割り当ては段階1の最初に行う
2. 一致テストの正解データを `Days/day113/Golden/` に作成（使い方は `Golden/README.md`）。段階2の採点移植は、EditModeテストでこれと比べて合格させる
