#!/usr/bin/env python3
"""
GameMacroAssistant Workflow State Machine
中断復帰対応のワークフロー状態管理システム
"""

import json
import os
from datetime import datetime, timezone
from typing import Dict, List, Optional, Tuple
try:
    from .SignalParser import validate_completion_signal, SignalParser
    from .ArtifactValidator import ArtifactValidator
    from .ProgressManager import ProgressManager
except ImportError:
    # 直接実行時のフォールバック
    import sys
    import os
    sys.path.append(os.path.dirname(__file__))
    from SignalParser import validate_completion_signal, SignalParser
    from ArtifactValidator import ArtifactValidator
    from ProgressManager import ProgressManager

class WorkflowStateMachine:
    def __init__(self, progress_file_path: str = ".claude/progress.json"):
        self.progress_file = progress_file_path
        self.progress_data = None
        self.progress_manager = ProgressManager(progress_file_path)
        self.load_progress()
    
    def load_progress(self) -> bool:
        """progress.jsonファイルを読み込む"""
        try:
            if os.path.exists(self.progress_file):
                with open(self.progress_file, 'r', encoding='utf-8') as f:
                    self.progress_data = json.load(f)
                return True
            else:
                print(f"ERROR: {self.progress_file} が見つかりません")
                return False
        except json.JSONDecodeError as e:
            print(f"ERROR: JSON形式が不正です: {e}")
            return False
        except Exception as e:
            print(f"ERROR: ファイル読み込みエラー: {e}")
            return False
    
    def get_current_phase(self) -> str:
        """現在のフェーズを判定"""
        if not self.progress_data:
            return "unknown"
        
        # 中断された作業がある場合
        if self.has_interrupted_work():
            return "resuming_work"
        
        current_phase = self.progress_data.get("current_phase", "unknown")
        
        # 詳細フェーズ判定
        workflow_state = self.progress_data.get("workflow_state", {})
        active_tasks = self.progress_data.get("active_tasks", {})
        working_tasks = self.progress_data.get("current_working_tasks", {})
        
        if working_tasks:
            return "development_in_progress"
        elif workflow_state.get("user_test_pending"):
            return "user_testing"
        elif any(task.get("review_status") == "pending" for task in active_tasks.values()):
            return "code_review"
        elif workflow_state.get("ready_for_next_sprint"):
            return "planning_next_sprint"
        else:
            return current_phase
    
    def has_interrupted_work(self) -> bool:
        """中断された作業があるかチェック"""
        if not self.progress_data:
            return False
        
        working_tasks = self.progress_data.get("current_working_tasks", {})
        session_context = self.progress_data.get("session_context", {})
        
        # 作業中タスクがある、または前回中断の記録がある
        return bool(working_tasks) or session_context.get("interruption_cause") is not None
    
    def get_interrupted_tasks(self) -> Dict:
        """中断されたタスクの詳細を取得"""
        if not self.progress_data:
            return {}
        
        return self.progress_data.get("current_working_tasks", {})
    
    def get_next_actions(self) -> List[Tuple[str, str]]:
        """次に実行すべきアクションを決定"""
        phase = self.get_current_phase()
        actions = []
        
        if phase == "resuming_work":
            # 中断復帰アクション
            interrupted_tasks = self.get_interrupted_tasks()
            for task_id, task_data in interrupted_tasks.items():
                current_step = task_data.get("current_step", "implementation")
                assignee = task_data.get("assignee", "dev-agent")
                actions.append((assignee, f"resume {task_id} at {current_step}"))
        
        elif phase == "development_in_progress":
            # 開発継続アクション
            working_tasks = self.progress_data.get("current_working_tasks", {})
            for task_id, task_data in working_tasks.items():
                assignee = task_data.get("assignee", "dev-agent")
                actions.append((assignee, f"continue {task_id}"))
        
        elif phase == "planning" or phase == "planning_next_sprint":
            actions.append(("planner-agent", "create sprint plan"))
        
        elif phase == "development":
            # 利用可能タスクの開発
            next_tasks = self.progress_data.get("next_available_tasks", [])
            active_agents = self.progress_data.get("workflow_state", {}).get("active_agents", {})
            
            idle_agents = [agent for agent, data in active_agents.items() 
                          if data.get("status") == "idle"]
            
            for i, task in enumerate(next_tasks[:len(idle_agents)]):
                agent = idle_agents[i] if i < len(idle_agents) else "dev-agent"
                actions.append((agent, f"implement {task}"))
        
        elif phase == "code_review":
            actions.append(("review-agent", "review completed tasks"))
        
        elif phase == "user_testing":
            actions.append(("user-test-coordinator", "coordinate user testing"))
        
        elif phase == "integration":
            actions.append(("integrator-agent", "integrate approved tasks"))
        
        return actions if actions else [("main-agent", "analyze current situation")]
    
    def get_workflow_summary(self) -> Dict:
        """ワークフロー状況の要約を取得"""
        if not self.progress_data:
            return {"error": "progress data not loaded"}
        
        phase = self.get_current_phase()
        next_actions = self.get_next_actions()
        
        summary = {
            "current_phase": phase,
            "next_actions": next_actions,
            "has_interrupted_work": self.has_interrupted_work(),
            "project_status": {
                "total_tasks": self.progress_data.get("project_metrics", {}).get("total_tasks_planned", 0),
                "completed_tasks": self.progress_data.get("project_metrics", {}).get("completed_tasks", 0),
                "in_progress_tasks": self.progress_data.get("project_metrics", {}).get("in_progress_tasks", 0),
                "current_sprint": self.progress_data.get("current_sprint", "unknown")
            }
        }
        
        # 中断作業の詳細を追加
        if self.has_interrupted_work():
            summary["interrupted_tasks"] = self.get_interrupted_tasks()
            summary["session_context"] = self.progress_data.get("session_context", {})
        
        return summary
    
    def can_proceed_to(self, next_action: str) -> bool:
        """指定されたアクションが実行可能かチェック"""
        current_phase = self.get_current_phase()
        
        # フェーズ遷移ルール
        phase_transitions = {
            "planning": ["development", "task_generation"],
            "development": ["code_review", "development_in_progress"],
            "development_in_progress": ["code_review", "development"],
            "code_review": ["development", "user_testing", "test_documentation"],
            "user_testing": ["integration", "bug_fixing"],
            "integration": ["planning_next_sprint", "completed"],
            "resuming_work": ["development_in_progress", "code_review"]
        }
        
        allowed_transitions = phase_transitions.get(current_phase, [])
        return next_action in allowed_transitions or next_action == "error_handling"
    
    def handle_completion_signal(self, raw_signal: str) -> Tuple[bool, str]:
        """
        エージェントからの完了シグナルを処理
        
        Returns:
            (success, message)
        """
        # シグナル検証
        is_valid, message, parsed_signal = validate_completion_signal(raw_signal)
        
        if not is_valid:
            return False, f"Signal validation failed: {message}"
        
        # TaskID抽出
        parser = SignalParser()
        task_id = parser.extract_task_id_from_evidence(parsed_signal.evidence)
        if not task_id:
            return False, "TaskID could not be extracted from evidence"
        
        # シグナル種別ごとの処理
        signal_type = parsed_signal.signal_type
        
        try:
            if signal_type == "##DEV_DONE##":
                return self._handle_dev_done(task_id, parsed_signal.evidence)
            elif signal_type == "##REVIEW_PASS##":
                return self._handle_review_pass(task_id, parsed_signal.evidence)
            elif signal_type == "##REVIEW_FAIL##":
                return self._handle_review_fail(task_id, parsed_signal.evidence)
            elif signal_type == "##TESTDOC_COMPLETE##":
                return self._handle_testdoc_complete(task_id, parsed_signal.evidence)
            elif signal_type.endswith("_FAILED##"):
                return self._handle_failure_signal(task_id, parsed_signal.evidence, signal_type)
            else:
                return False, f"Unknown signal type: {signal_type}"
                
        except Exception as e:
            return False, f"Signal processing error: {e}"
    
    def _handle_dev_done(self, task_id: str, evidence: Dict) -> Tuple[bool, str]:
        """Dev-Agent完了シグナル処理（成果物検証を含む）"""
        # 1. 証跡の基本検証
        validator = ArtifactValidator()
        evidence_valid, evidence_errors = validator.validate_agent_evidence(evidence)
        
        if not evidence_valid:
            return False, f"Evidence validation failed: {'; '.join(evidence_errors)}"
        
        # 2. 実際の成果物検証（重要！）
        artifacts_valid, validation_message, validation_details = validator.validate_task_completion(task_id)
        
        if not artifacts_valid:
            print(f"[DEV_DONE_REJECTED] Task {task_id} artifacts validation failed:")
            print(f"  Reason: {validation_message}")
            for error in validation_details.get("errors", []):
                print(f"  - {error}")
            
            # Dev-Agentに詳細な再作業指示を送信
            return False, f"Artifact validation failed: {validation_message}. Please verify actual implementation exists and builds successfully."
        
        # 3. Progress更新（中央管理）
        try:
            evidence_for_progress = {
                "completion_evidence": validation_details,
                "validation_timestamp": datetime.now().isoformat(),
                "implementation_files": evidence.get("files", []),
                "test_results": validation_details.get("test_results", {})
            }
            
            self.progress_manager.update_task_status(
                task_id, 
                "review_pending", 
                evidence_for_progress,
                "dev_done_signal_processed"
            )
            
        except Exception as e:
            print(f"Warning: Progress update failed: {e}")
        
        # 4. 成果物検証成功 - Review-Agentに移行
        print(f"[DEV_DONE] Task {task_id} fully validated. Moving to review phase.")
        print(f"  Build: ✅ Success")
        print(f"  Tests: ✅ {validation_details.get('test_results', {}).get('passed_count', 0)} passed")
        print(f"  Files: ✅ All required files present")
        
        return True, f"Task {task_id} ready for review - all artifacts verified"
    
    def _handle_review_pass(self, task_id: str, evidence: Dict) -> Tuple[bool, str]:
        """Review-Agent承認シグナル処理"""
        coverage = evidence.get("coverage_percent")
        issues = evidence.get("issues_found", "0")
        
        print(f"[REVIEW_PASS] Task {task_id} approved. Coverage: {coverage}%, Issues: {issues}")
        
        # TestDoc-Agentに移行
        return True, f"Task {task_id} ready for test documentation"
    
    def _handle_review_fail(self, task_id: str, evidence: Dict) -> Tuple[bool, str]:
        """Review-Agent却下シグナル処理"""
        reasons = evidence.get("failure_reasons", [])
        issues_count = evidence.get("issues_found", "unknown")
        
        print(f"[REVIEW_FAIL] Task {task_id} rejected. Issues: {issues_count}, Reasons: {reasons}")
        
        # Dev-Agentに差し戻し
        return True, f"Task {task_id} needs rework: {', '.join(reasons)}"
    
    def _handle_testdoc_complete(self, task_id: str, evidence: Dict) -> Tuple[bool, str]:
        """TestDoc-Agent完了シグナル処理"""
        test_file = evidence.get("test_file_path")
        test_count = evidence.get("test_count")
        estimated_time = evidence.get("estimated_minutes")
        
        if not os.path.exists(test_file):
            return False, f"Test document not found: {test_file}"
        
        # Progress更新 - ユーザーテスト待ちに追加
        try:
            self.progress_manager.add_user_test_pending(task_id, evidence)
        except Exception as e:
            print(f"Warning: User test pending update failed: {e}")
        
        print(f"[TESTDOC_COMPLETE] Task {task_id} test doc ready. {test_count} tests, ~{estimated_time} min")
        
        # User-Test-Coordinatorに移行
        return True, f"Task {task_id} ready for user testing"
    
    def _handle_failure_signal(self, task_id: str, evidence: Dict, signal_type: str) -> Tuple[bool, str]:
        """失敗シグナル処理"""
        failure_reason = evidence.get("failure_reason", "unknown")
        error_details = evidence.get("error_details", "")
        
        print(f"[{signal_type}] Task {task_id} failed: {failure_reason}")
        if error_details:
            print(f"[Details] {error_details}")
        
        # BugFix-Agentやエラーハンドリングに移行
        return True, f"Task {task_id} failure recorded, requires intervention"

def main():
    """メイン実行関数"""
    print("GameMacroAssistant Workflow State Machine")
    print("=" * 50)
    
    # WorkflowStateMachineを初期化
    workflow = WorkflowStateMachine()
    
    if not workflow.progress_data:
        print("ERROR: progress.jsonの読み込みに失敗しました")
        return
    
    # 現在の状況を分析
    summary = workflow.get_workflow_summary()
    
    print(f"[Current Phase] {summary['current_phase']}")
    print(f"[Project] {workflow.progress_data.get('project_id', 'Unknown')}")
    print(f"[Sprint] {summary['project_status']['current_sprint']}")
    print()
    
    # Progress Status
    status = summary['project_status']
    if status['total_tasks'] > 0:
        progress_pct = (status['completed_tasks'] / status['total_tasks']) * 100
        progress_bar = "#" * int(progress_pct // 10) + "-" * (10 - int(progress_pct // 10))
        print(f"[Progress] {progress_bar} {progress_pct:.1f}% ({status['completed_tasks']}/{status['total_tasks']})")
    
    print(f"[In Progress Tasks] {status['in_progress_tasks']}")
    print()
    
    # Check for interrupted work
    if summary['has_interrupted_work']:
        print("[Interrupted Work Found]")
        for task_id, task_data in summary.get('interrupted_tasks', {}).items():
            interruption = task_data.get('interruption_point', {})
            print(f"   {task_id}: {task_data.get('description', 'Unknown task')}")
            print(f"   Progress: {task_data.get('progress_details', {}).get('implementation_progress', 'Unknown')}")
            print(f"   Interrupted at: {interruption.get('timestamp', 'Unknown')}")
            print(f"   Next action: {interruption.get('next_action', 'Unknown')}")
        print()
    
    # Recommended Actions
    print("[Recommended Actions]")
    for i, (agent, action) in enumerate(summary['next_actions'], 1):
        print(f"   {i}. {agent}: {action}")
    
    print()
    print("[Analysis Complete]")

if __name__ == "__main__":
    main()