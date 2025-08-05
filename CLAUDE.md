# GameMacroAssistant - AI開発ワークフロー管理システム

## プロジェクト概要
GameMacroAssistantは、Windows 11マクロ自動化ツールの開発プロジェクトです。AIエージェントによる高速な実装・検証ワークフローにより、効率的な開発サイクルを実現します。

## 技術要件
- **言語**: C# (.NET 8)
- **UI Framework**: WPF with MVVM pattern
- **MVVM Library**: CommunityToolkit.Mvvm
- **テスト**: xUnit, Coverlet, ReportGenerator
- **Windows API**: user32.dll, kernel32.dll, dxgi.dll統合
- **アーキテクチャ**: 疎結合設計、依存性注入、SOLID原則

## ディレクトリ構成
```
/
├── src/                    # アプリケーション本体
│   ├── GameMacroAssistant.Core/      # ビジネスロジック層
│   ├── GameMacroAssistant.Wpf/       # プレゼンテーション層
│   └── GameMacroAssistant.Tests/     # 単体・統合テスト
├── docs/                   # プロジェクトドキュメント
│   ├── pm/                 # プロジェクト管理
│   │   ├── tasks/          # TaskGen-Agent生成タスク仕様
│   │   ├── sprints/        # Planner-Agent生成開発計画
│   │   └── progress/       # 進捗管理JSONファイル
│   ├── user-tests/         # TestDoc-Agent生成ユーザーテスト
│   └── requirements/       # 要件定義・技術仕様
├── worktrees/              # Dev-Agent作業ディレクトリ（Git Worktree）
└── .claude/
    ├── agents/             # サブエージェント定義
    ├── progress.json       # 全体進捗管理
    └── settings.local.json # Claude設定
```

## ブランチ戦略
- **main**: 保護ブランチ、統合済み安定版
- **task-<TaskID>-<description>**: 個別タスク開発用（worktreeと対応）
- **hotfix-<ID>**: 緊急バグ修正用（BugFix-Agent専用）

## 開発制約・設計原則
### アーキテクチャ制約
- **疎結合設計**: 各レイヤー間の依存性を最小化
- **依存性注入**: Microsoft.Extensions.DependencyInjection使用
- **SOLID原則**: 単一責任、開放閉鎖、リスコフ置換、インターフェース分離、依存性逆転
- **テスタビリティ**: 80%以上のテストカバレッジ維持

### 実装制約
- **Windows API統合**: P/Invoke経由でネイティブAPI呼び出し
- **パフォーマンス**: 画面キャプチャ50ms以内、入力精度≤5ms
- **メモリ管理**: using文とIDisposableパターンの徹底
- **スレッドセーフティ**: ConcurrentCollectionとlock文の適切な使用

### ファイル・ディレクトリ制約
- **Claude Code制限**: 上位ディレクトリへの移動不可のため、worktreesはルート配下に配置
- **相対パス使用**: 絶対パスは環境依存回避のため使用禁止
- **ファイル命名**: TaskID-機能名-詳細の形式（例：T-001-core-models）

## ワークフロー管理システム

### 進捗管理JSON構造
```json
{
  "project_id": "GameMacroAssistant",
  "current_phase": "planning|development|review|testing|integration",
  "current_sprint": "sprint-01",
  "completed_sprints": ["sprint-01"],
  "active_tasks": {
    "T-001": {
      "status": "completed|in_progress|blocked|pending",
      "assignee": "dev-agent-1",
      "worktree_path": "worktrees/T-001",
      "branch_name": "task-T-001-core-models",
      "dependencies": ["T-000"],
      "completion_date": "2025-01-15",
      "review_status": "approved|pending|rejected"
    }
  },
  "next_available_tasks": ["T-002", "T-003"],
  "workflow_state": {
    "last_completed_step": "7",
    "next_step": "2",
    "waiting_for_user": false,
    "user_test_pending": []
  }
}
```

## AI開発ワークフロー

### フェーズ1: 要件取得・計画立案
1. **要件定義書取得**: ユーザーから固まった要件定義を受領
2. **進捗状態確認**: `.claude/progress.json`を読み込み、前回中断箇所を特定
3. **要件分析**: Intake-Agentが要件を構造化・分析
4. **技術仕様作成**: DocGen-Agentがclaude.md更新・技術要件定義
5. **開発計画立案**: Planner-Agentが依存関係を分析し実行可能タスクを抽出

### フェーズ2: タスク管理・配備
6. **タスク仕様作成**: TaskGen-Agentが実装手順・背景・テスト要件を含む詳細仕様を生成
7. **開発環境配備**: Dispatcher-Agentがworktree作成・Dev-Agent割り当て

### フェーズ3: 並行開発・品質保証  
8. **並行実装**: 複数Dev-Agentが独立してworktree内で開発・テスト実行（同時実行は最大2つまで）
9. **コードレビュー**: Review-Agentが実装品質・設計適合性を検証・修正指示
10. **ユーザーテスト準備**: TestDoc-Agentがテスト手順書・worktree情報を含むMDファイル作成

### フェーズ4: ユーザー検証・統合
11. **ユーザーテスト**: ユーザーによる機能検証・バグ報告受付
12. **バグ修正サイクル**: BugFix-Agentがログ分析→問題特定→修正実装を反復
13. **統合準備**: 全テスト完了後、main→taskブランチプル・結合テスト実行
14. **本統合**: Integrator-Agentがtask→mainマージ・統合検証

### フェーズ5: 次サイクル移行
15. **進捗更新**: 完了タスクをprogress.jsonに記録
16. **次回計画**: 新たに実行可能になったタスクでフェーズ1に戻る

## メインエージェント統括機能

### ワークフロー制御（主エージェント責任）
- **フェーズ自動判定**: progress.jsonの状態分析による次フェーズ決定
- **エージェント自動起動**: 現在フェーズに応じた適切なサブエージェント呼び出し
- **依存関係自動管理**: タスク間依存関係の追跡・実行順序制御
- **ブロッカー自動検出**: 作業停滞要因の特定・解決策自動提示

### 進捗監視・制御
- **状態リアルタイム把握**: progress.json + sprint files継続監視
- **メトリクス活用判断**: テストカバレッジ・ベロシティによる品質ゲート
- **並行処理制御**: Dev-Agent同時実行制限（最大2）の動的管理
- **ユーザー介入最小化**: 自動化可能な判断の機械実行

### 品質保証自動制御
- **統合条件自動チェック**: 完了基準（カバレッジ80%+、テスト全通過）検証
- **フェーズゲート制御**: 条件未達時の自動ブロック・改善指示
- **エスカレーション自動判定**: 人的介入必要時の明確化・通知

## エージェント定義
- **Intake-Agent** (file=".claude/agents/intake-agent.md"): 要件定義の構造化・分析
- **DocGen-Agent** (file=".claude/agents/docgen-agent.md"): 技術仕様・プロジェクト文書生成  
- **Planner-Agent** (file=".claude/agents/planner-agent.md"): 開発計画・依存関係分析
- **TaskGen-Agent** (file=".claude/agents/taskgen-agent.md"): タスク仕様書・実装手順生成
- **Dispatcher-Agent** (file=".claude/agents/dispatcher-agent.md"): worktree配備・Dev-Agent割り当て
- **Dev-Agent** (file=".claude/agents/dev-agent.md", count="最大2", concurrent_limit=2): 並行実装・単体テスト実行
- **Review-Agent** (file=".claude/agents/review-agent.md"): コード品質検証・修正指示
- **TestDoc-Agent** (file=".claude/agents/testdoc-agent.md"): ユーザーテスト手順書生成
- **User-Test-Coordinator** (file=".claude/agents/user-test-coordinator-agent.md"): ユーザーテスト管理・バグ修正サイクル調整
- **BugFix-Agent** (file=".claude/agents/bugfix-agent.md"): バグ分析・修正実装
- **Integrator-Agent** (file=".claude/agents/integrator-agent.md"): ブランチ統合・結合テスト

## 成功基準
- **開発効率**: 従来比3倍の開発速度達成
- **品質基準**: 80%以上のテストカバレッジ・ゼロクリティカルバグ
- **保守性**: SOLID原則準拠・疎結合アーキテクチャによる高保守性
- **ユーザー満足度**: 要件定義との完全適合・直感的UI実現