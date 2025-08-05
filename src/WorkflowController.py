#!/usr/bin/env python3
"""
ワークフロー制御システム - フェーズ自動判定とタスク割り当てロジック
GameMacroAssistant AI開発ワークフロー管理
"""

import json
import os
from typing import Dict, List, Optional, Tuple
from enum import Enum
from datetime import datetime

class ProjectPhase(Enum):
    PLANNING = "planning"
    DEVELOPMENT = "development" 
    REVIEW = "review"
    TESTING = "testing"
    INTEGRATION = "integration"

class TaskStatus(Enum):
    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    COMPLETED = "completed"
    BLOCKED = "blocked"

class WorkflowController:
    """メインエージェント統括機能 - ワークフロー自動制御"""
    
    def __init__(self, project_root: str = "."):
        self.project_root = project_root
        self.progress_file = os.path.join(project_root, ".claude", "progress.json")
        self.max_concurrent_devs = 2
        
    def load_progress(self) -> Dict:
        """進捗状況をJSONから読み込み"""
        try:
            with open(self.progress_file, 'r', encoding='utf-8') as f:
                return json.load(f)
        except FileNotFoundError:
            return self._init_progress()
    
    def save_progress(self, progress: Dict) -> None:
        """進捗状況をJSONに保存"""
        with open(self.progress_file, 'w', encoding='utf-8') as f:
            json.dump(progress, f, indent=2, ensure_ascii=False)
    
    def _init_progress(self) -> Dict:
        """初期進捗状況を生成"""
        return {
            "project_id": "GameMacroAssistant",
            "last_updated": datetime.now().isoformat(),
            "current_phase": "planning",
            "current_sprint": "sprint-01",
            "completed_sprints": [],
            "active_tasks": {},
            "next_available_tasks": [],
            "workflow_state": {
                "last_completed_step": "0",
                "next_step": "1", 
                "waiting_for_user": False,
                "user_test_pending": []
            },
            "project_metrics": {
                "total_tasks_planned": 0,
                "completed_tasks": 0,
                "in_progress_tasks": 0,
                "test_coverage": "0%",
                "sprint_velocity": 0
            }
        }
    
    def determine_current_phase(self, progress: Dict) -> ProjectPhase:
        """現在のプロジェクト状態を分析してフェーズを自動判定"""
        active_tasks = progress.get("active_tasks", {})
        next_available = progress.get("next_available_tasks", [])
        workflow_state = progress.get("workflow_state", {})
        
        # 統合準備完了チェック
        integration_completed = workflow_state.get("integration_completed", [])
        ready_for_next_sprint = workflow_state.get("ready_for_next_sprint", False)
        
        # 進行中タスクの状態分析
        in_progress_count = sum(1 for task in active_tasks.values() 
                               if task.get("status") == "in_progress")
        completed_count = sum(1 for task in active_tasks.values() 
                             if task.get("status") == "completed")
        pending_tests = workflow_state.get("user_test_pending", [])
        
        # フェーズ判定ロジック
        if ready_for_next_sprint and next_available:
            return ProjectPhase.PLANNING
        elif in_progress_count > 0:
            return ProjectPhase.DEVELOPMENT
        elif completed_count > 0 and not integration_completed:
            return ProjectPhase.REVIEW
        elif pending_tests:
            return ProjectPhase.TESTING
        elif integration_completed and not ready_for_next_sprint:
            return ProjectPhase.INTEGRATION
        else:
            return ProjectPhase.PLANNING
    
    def get_next_actions(self, progress: Dict) -> List[Tuple[str, Dict]]:
        """現在フェーズに基づいて次に実行すべきアクションを決定"""
        current_phase = self.determine_current_phase(progress)
        actions = []
        
        if current_phase == ProjectPhase.PLANNING:
            # 新規スプリント計画が必要
            if progress.get("next_available_tasks"):
                actions.append(("planner-agent", {
                    "action": "create_next_sprint",
                    "available_tasks": progress["next_available_tasks"]
                }))
        
        elif current_phase == ProjectPhase.DEVELOPMENT:
            # 開発タスクの並行実行管理
            actions.extend(self._get_development_actions(progress))
        
        elif current_phase == ProjectPhase.REVIEW:
            # コードレビュー実行
            completed_tasks = [
                task_id for task_id, task in progress["active_tasks"].items()
                if task.get("status") == "completed" and task.get("review_status") != "approved"
            ]
            for task_id in completed_tasks:
                actions.append(("review-agent", {
                    "action": "review_task",
                    "task_id": task_id
                }))
        
        elif current_phase == ProjectPhase.TESTING:
            # ユーザーテスト準備・実行
            actions.append(("testdoc-agent", {
                "action": "prepare_user_tests",
                "completed_tasks": progress.get("workflow_state", {}).get("integration_completed", [])
            }))
        
        elif current_phase == ProjectPhase.INTEGRATION:
            # ブランチ統合実行
            ready_tasks = [
                task_id for task_id, task in progress["active_tasks"].items()
                if task.get("review_status") == "approved"
            ]
            if ready_tasks:
                actions.append(("integrator-agent", {
                    "action": "integrate_branches", 
                    "task_list": ready_tasks
                }))
        
        return actions
    
    def _get_development_actions(self, progress: Dict) -> List[Tuple[str, Dict]]:
        """開発フェーズでの並行タスク管理"""
        actions = []
        active_tasks = progress.get("active_tasks", {})
        
        # 現在の進行中タスク数をカウント
        in_progress_tasks = [
            task_id for task_id, task in active_tasks.items()
            if task.get("status") == "in_progress"
        ]
        
        # 新規タスクアサイン可能かチェック
        if len(in_progress_tasks) < self.max_concurrent_devs:
            pending_tasks = [
                task_id for task_id, task in active_tasks.items()
                if task.get("status") == "pending"
            ]
            
            # 利用可能なDev-Agentスロット分だけタスクをアサイン
            available_slots = self.max_concurrent_devs - len(in_progress_tasks)
            for i, task_id in enumerate(pending_tasks[:available_slots]):
                agent_id = f"dev-agent-{i+1}"
                actions.append(("dev-agent", {
                    "action": "implement_task",
                    "task_id": task_id,
                    "agent_id": agent_id
                }))
        
        return actions
    
    def update_task_status(self, task_id: str, new_status: str, 
                          assignee: Optional[str] = None) -> None:
        """タスク状態の更新"""
        progress = self.load_progress()
        
        if task_id in progress["active_tasks"]:
            progress["active_tasks"][task_id]["status"] = new_status
            if assignee:
                progress["active_tasks"][task_id]["assignee"] = assignee
            
            # メトリクス更新
            self._update_metrics(progress)
            
            # 進捗状況保存
            progress["last_updated"] = datetime.now().isoformat()
            self.save_progress(progress)
    
    def _update_metrics(self, progress: Dict) -> None:
        """プロジェクトメトリクスの自動更新"""
        active_tasks = progress.get("active_tasks", {})
        
        completed_count = sum(1 for task in active_tasks.values() 
                             if task.get("status") == "completed")
        in_progress_count = sum(1 for task in active_tasks.values() 
                               if task.get("status") == "in_progress")
        
        progress["project_metrics"]["completed_tasks"] = completed_count
        progress["project_metrics"]["in_progress_tasks"] = in_progress_count
        progress["project_metrics"]["total_tasks_planned"] = len(active_tasks)
    
    def check_quality_gates(self, progress: Dict) -> Dict[str, bool]:
        """品質ゲートの自動チェック"""
        metrics = progress.get("project_metrics", {})
        coverage_str = metrics.get("test_coverage", "0%")
        
        # カバレッジ数値抽出
        try:
            coverage = float(coverage_str.replace("%", ""))
        except:
            coverage = 0.0
        
        return {
            "test_coverage_ok": coverage >= 80.0,
            "no_critical_bugs": True,  # TODO: バグトラッキング統合
            "all_tests_passing": True,  # TODO: CI/CD統合
            "ready_for_integration": coverage >= 80.0
        }
    
    def get_workflow_summary(self) -> Dict:
        """現在のワークフロー状態サマリー"""
        progress = self.load_progress()
        current_phase = self.determine_current_phase(progress)
        next_actions = self.get_next_actions(progress)
        quality_gates = self.check_quality_gates(progress)
        
        return {
            "current_phase": current_phase.value,
            "next_actions": next_actions,
            "quality_status": quality_gates,
            "progress_summary": {
                "completed_tasks": progress["project_metrics"]["completed_tasks"],
                "total_tasks": progress["project_metrics"]["total_tasks_planned"],
                "test_coverage": progress["project_metrics"]["test_coverage"],
                "sprint_velocity": progress["project_metrics"]["sprint_velocity"]
            }
        }

# 使用例・テスト実行
if __name__ == "__main__":
    controller = WorkflowController()
    summary = controller.get_workflow_summary()
    print("=== ワークフロー制御システム ===")
    print(f"現在フェーズ: {summary['current_phase']}")
    print(f"次のアクション: {len(summary['next_actions'])}件")
    for action_type, params in summary['next_actions']:
        print(f"  - {action_type}: {params.get('action', 'N/A')}")
    print(f"品質ゲート: {summary['quality_status']}")