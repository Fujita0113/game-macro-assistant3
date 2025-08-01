# 要件定義 v3.3

最終レビューに基づき、すべての指摘事項を修正・追補した、開発着手のための確定版要件です。

|ID|要件|機能グループ|
|---|---|---|
|**マクロ記録**|||
|R-001|ユーザーはUI上のボタンクリックによってマクロの記録を開始できる。|マクロ記録|
|R-002|マウス操作のスクリーン絶対座標(px)、ボタン種別、押下時間を記録する。|マクロ記録|
|R-003|キーボード操作の仮想キーコード、およびキーの押下・離上時刻を記録する。|マクロ記録|
|R-004|アプリの入力キューがイベントを受信後50ms以内にPNG(**アクティブディスプレイのネイティブ解像度**)を取得し、失敗時は10msバックオフで最大2回リトライ、最終失敗時は`Err-CAP`を記録する。|マクロ記録|
|R-005|記録停止のデフォルトキーはESCとし、ユーザーは設定画面で変更できる。|マクロ記録|
|R-006|Desktop Duplication API失敗時は最大15 FPSでGDI BitBltにフォールバックし、**半透明のウォーターマーク"CaptureLimited"を重畳**、通知センターとログに`Err-CAP`を記録する。|マクロ記録|
|**ビジュアルエディタ**|||
|R-007|記録終了後、操作がブロック化されたビジュアルエディタが自動的に開く。|ビジュアルエディタ|
|R-008|ドラッグによる矩形選択で条件画像を編集でき、UIの詳細はMVP後のUI/UX設計フェーズで定義する。|ビジュアルエディタ|
|R-009|2秒以内に完了した複合操作はUndo/Redo上で1操作とし、この秒数は`Settings > Editor`で**0.5s～5.0s**の範囲で変更可能とする。|ビジュアルエディタ|
|R-010|ブロックのドラッグ＆ドロップによる順序変更は垂直リストにスナップし、失敗した場合は操作をキャンセルして元の位置に戻す。|ビジュアルエディタ|
|R-011|パラメータ編集時、クリック座標はプライマリディスプレイの解像度内に制限し、無効な値の保存をブロックする。|ビジュアルエディタ|
|**マクロ実行**|||
|R-012|グローバルホットキーの競合時は代替候補を3つ提示し、ユーザーが選択して保存するまで登録は行わない。|マクロ実行|
|R-013|画像一致判定の閾値は、設定画面のスライダー（**デフォルト: SSIM 0.95, ピクセル差 3%**）で変更可能とする。|マクロ実行|
|R-014|再生誤差が**平均≤5ms、最大≤15ms**の基準を超過した場合、`Err-TIM`をログに記録する。|マクロ実行|
|R-015|タイムアウト時はエラーコード付きトーストを表示し、同時にログファイル(`%APPDATA%\GameMacroAssistant\Logs\YYYY-MM-DD.log`)にJSON形式で詳細を追記する。|マクロ実行|
|R-016|ツールチップはステップ完了時に即時更新するが、UIのチラつき防止のため**最大更新レートを10 FPS**に制限する。|マクロ実行|
|**プロジェクト管理とセキュリティ**|||
|R-017|マクロファイル（`.gma.json`）にはスキーマバージョンを含め、その定義は`docs/schema/macro_v1.json`に配置する。|プロジェクト管理|
|R-018|パスフレーズは**8桁以上**を必須とし、マクロ読み込み時に3回失敗した場合は読み込みをキャンセルする。|セキュリティ|
|R-019|クラッシュダンプは`%LOCALAPPDATA%\…\CrashDumps\`に保存し、ユーザー同意の上で送信する。同意文言と送信先URLは`docs/privacy_policy.md`で定義する。|品質保証|
|**品質・環境・アクセシビリティ**|||
|R-020|パフォーマンス閾値（CPU≤15%, RAM≤300MB）超過時は、進捗バーを赤く表示し**「High Load」トースト**で通知する。|パフォーマンス|
|R-021|すべての対話可能なUIコントロールはスクリーンリーダー用のUIAプロパティを公開する。|アクセシビリティ|
|R-022|100%以外のDPIスケーリングに関する既知の制限事項と座標変換アルゴリズムの擬似コードを`docs/troubleshooting.md`に明記する。|環境|
|R-023|GitHub Actions上で`choco install winappdriver --version 1.6.2`を実行し、対話モードと5秒待機を挟んでUIテストを実行する。|品質保証|
|R-024|CoverletとReportGeneratorでテストカバレッジを測定し、Coreレイヤーで80%以上を維持、結果は`CODECOV_TOKEN`（GitHub Secrets管理）を用いてCodecovへアップロードする。|品質保証|
|R-025|UIを表示せずマクロを実行するCLIオプション（`--headless`）を提供する。|自動テスト|

---

### Claude Code向け初期プロンプト

```
### ROLE
あなたはC#とWPF、そしてWindows APIの扱いに長けたシニアソフトウェアエンジニアです。

### CONTEXT
これから、Windows 11向けのPCゲーム操作を自動化するマクロツール「GameMacroAssistant」の新規開発を開始します。プロジェクトの目的は、プログラミング知識がないライトユーザーでも直感的にマクロを作成・編集できるツールを提供することです。
要件は「要件定義 v3.3」として完全に確定しています。

### TASK
確定した「要件定義 v3.3」に基づき、このプロジェクトの初期ソリューション構造と、コア機能の基本的なコードをC#で生成してください。

**技術スタック:**
* **言語・フレームワーク:** C# / .NET 8 / WPF
* **アーキテクチャ:** MVVM (Model-View-ViewModel)
* **ライブラリ:** CommunityToolkit.Mvvm (推奨)

**生成物:**
1.  **ディレクトリ構造:** 以下の構成でディレクトリとプロジェクトファイル（.csproj）を作成してください。
    ```
    /GameMacroAssistant
    |-- GameMacroAssistant.sln
    |-- /src
    |   |-- /GameMacroAssistant.Core
    |   |   |-- GameMacroAssistant.Core.csproj
    |   |   |-- /Models (Macro, Step, etc.)
    |   |   |-- /Services (IMacroRecorder, IImageMatcher, etc.)
    |   |-- /GameMacroAssistant.Wpf
    |   |   |-- GameMacroAssistant.Wpf.csproj
    |   |   |-- /Views (MainView.xaml, EditorView.xaml)
    |   |   |-- /ViewModels (MainViewModel.cs, EditorViewModel.cs)
    |   |   |-- App.xaml, App.xaml.cs
    |   |-- /GameMacroAssistant.Tests
    |   |   |-- GameMacroAssistant.Tests.csproj
    |-- /docs
    |   |-- /schema
    |   |   |-- macro_v1.json
    |   |   |-- log_schema.json
    |   |-- troubleshooting.md
    |   |-- privacy_policy.md
    |-- /.github/workflows
    |   |-- ci.yml
    ```
2.  **コードのスケルトン:**
    * 上記の各ファイルに、要件を満たすための基本的なクラス定義、インターフェース、プロパティ、メソッドシグネチャを記述してください。
    * ロジックが複雑になる部分には、処理内容を説明する`// TODO:`コメントを追加してください。
    * 特に、以下のコア機能の実装の土台となるコードを重点的に生成してください。
        * `GameMacroAssistant.Core/Services/IMacroRecorder.cs`: マウス・キーボード入力をフックし、イベントを記録するサービスのインターフェース。
        * `GameMacroAssistant.Core/Services/ScreenCaptureService.cs`: Desktop Duplication APIとGDI BitBltフォールバックによるスクリーンショット取得機能（R-006）。
        * `GameMacroAssistant.Core/Services/ImageMatcher.cs`: SSIMとピクセル差分による画像比較機能（R-013）。
        * `GameMacroAssistant.Wpf/ViewModels/EditorViewModel.cs`: マクロのブロックを表現する`ObservableCollection`と、Undo/Redo機能の基本的なロジック（R-009）。
    * `ci.yml`には、R-023, R-024で定義されたテストとカバレッジ測定のワークフローの骨子を記述してください。

**提供情報：要件定義 v3.3**
(ここに上記の要件定義v3.3のMarkdownテーブルを貼り付け)
```