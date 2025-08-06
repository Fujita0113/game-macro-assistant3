#!/usr/bin/env python3
"""
進捗管理の中央集権化モジュール

progress.jsonの更新をメインエージェントのみに限定し、
エージェント間の状態不整合を防止する。
"""

import json
import os
import threading
import inspect
from datetime import datetime
from typing import Dict, List, Optional, Any, Tuple
from pathlib import Path


class ProgressManager:
    """進捗状態の中央管理クラス"""
    
    def __init__(self, progress_file: str = ".claude/progress.json"):
        self.progress_file = Path(progress_file)
        self._lock = threading.Lock()
        self._authorized_callers = {"main-agent", "workflow-controller", "__main__"}
    
    def update_task_status(self, task_id: str, new_status: str, evidence: Dict, 
                          caller_context: Optional[str] = None) -> bool:
        """
        進捗状態を更新（認可されたエージェントのみ）
        
        Args:
            task_id: タスクID
            new_status: 新しいステータス
            evidence: 証跡情報
            caller_context: 呼び出し元コンテキスト（デバッグ用）
        
        Returns:
            更新成功かどうか
            
        Raises:
            PermissionError: 権限のないエージェントからの呼び出し
            ValueError: 不正な状態遷移
        """
        with self._lock:
            # 呼び出し元検証
            caller_agent = self._get_caller_agent()
            if not self._is_authorized_caller(caller_agent):
                raise PermissionError(
                    f"Unauthorized progress update attempt from '{caller_agent}'. "
                    f"Only main-agent can update progress.json"
                )
            
            # 証跡検証
            if not self._validate_status_evidence(task_id, new_status, evidence):
                raise ValueError(
                    f"Invalid evidence for status transition {task_id}: {new_status}"
                )
            
            # 進捗ファイル更新
            return self._update_progress_file(task_id, new_status, evidence, caller_context)
    
    def add_user_test_pending(self, task_id: str, evidence: Dict) -> bool:
        """ユーザーテスト待ちタスクを追加"""
        with self._lock:
            if not self._is_authorized_caller(self._get_caller_agent()):
                raise PermissionError("Only main-agent can add user test pending tasks")
            
            progress = self._load_progress()
            if "workflow_state" not in progress:
                progress["workflow_state"] = {}
            
            if "user_test_pending" not in progress["workflow_state"]:
                progress["workflow_state"]["user_test_pending"] = []
            
            if task_id not in progress["workflow_state"]["user_test_pending"]:
                progress["workflow_state"]["user_test_pending"].append(task_id)
                progress["last_updated"] = datetime.now().isoformat()
                return self._save_progress(progress)
            
            return True
    
    def remove_user_test_pending(self, task_id: str) -> bool:
        """ユーザーテスト待ちタスクを削除"""
        with self._lock:
            if not self._is_authorized_caller(self._get_caller_agent()):
                raise PermissionError("Only main-agent can remove user test pending tasks")
            
            progress = self._load_progress()
            pending_list = progress.get("workflow_state", {}).get("user_test_pending", [])
            
            if task_id in pending_list:
                pending_list.remove(task_id)
                progress["last_updated"] = datetime.now().isoformat()
                return self._save_progress(progress)
            
            return True
    
    def update_agent_status(self, agent_id: str, status: str, current_task: Optional[str] = None) -> bool:
        """エージェントステータスを更新"""
        with self._lock:
            if not self._is_authorized_caller(self._get_caller_agent()):
                raise PermissionError("Only main-agent can update agent status")
            
            progress = self._load_progress()
            
            if "workflow_state" not in progress:
                progress["workflow_state"] = {}
            if "active_agents" not in progress["workflow_state"]:
                progress["workflow_state"]["active_agents"] = {}
            
            agent_data = progress["workflow_state"]["active_agents"].get(agent_id, {})
            agent_data.update({
                "status": status,
                "current_task": current_task,
                "last_heartbeat": datetime.now().isoformat()
            })
            
            progress["workflow_state"]["active_agents"][agent_id] = agent_data
            progress["last_updated"] = datetime.now().isoformat()
            
            return self._save_progress(progress)
    
    def _get_caller_agent(self) -> str:
        """呼び出し元エージェントを特定"""
        # スタックフレームから呼び出し元を特定
        frame = inspect.currentframe()
        try:
            # 2レベル上のフレーム（update_task_status → _get_caller_agent）
            caller_frame = frame.f_back.f_back
            if caller_frame is None:
                return "unknown"
            
            # ファイル名から判定
            caller_file = caller_frame.f_code.co_filename
            
            if "WorkflowStateMachine" in caller_file or "__main__" in caller_frame.f_globals.get("__name__", ""):
                return "main-agent"
            elif "dev-agent" in caller_file:
                return "dev-agent"
            elif "review-agent" in caller_file:
                return "review-agent"
            elif "testdoc-agent" in caller_file:
                return "testdoc-agent"
            else:
                # ファイル名から推定
                filename = Path(caller_file).stem
                if "main" in filename.lower() or "workflow" in filename.lower():
                    return "main-agent"
                else:
                    return filename
                    
        finally:
            del frame
    
    def _is_authorized_caller(self, caller_agent: str) -> bool:
        """呼び出し元が認可されているかチェック"""
        return caller_agent in self._authorized_callers or caller_agent == "main-agent"
    
    def _validate_status_evidence(self, task_id: str, new_status: str, evidence: Dict) -> bool:
        """状態遷移の証跡妥当性検証"""
        # 必須フィールドの確認
        required_evidence_fields = {
            "completed": ["completion_evidence", "validation_timestamp"],
            "in_progress": ["start_timestamp", "assignee"],
            "review_pending": ["implementation_files", "test_results"],
            "user_test_pending": ["test_document_path", "estimated_time"]
        }
        
        required_fields = required_evidence_fields.get(new_status, [])
        for field in required_fields:
            if field not in evidence:
                return False
        
        # ファイル存在確認
        if "implementation_files" in evidence:
            for file_path in evidence["implementation_files"]:
                if not Path(file_path).exists():
                    return False
        
        if "test_document_path" in evidence:
            if not Path(evidence["test_document_path"]).exists():
                return False
        
        return True
    
    def _load_progress(self) -> Dict:
        """progress.jsonファイルを読み込み"""
        if not self.progress_file.exists():
            # デフォルト構造で初期化
            return {
                "project_id": "GameMacroAssistant",
                "last_updated": datetime.now().isoformat(),
                "current_phase": "development",
                "active_tasks": {},
                "current_working_tasks": {},
                "workflow_state": {
                    "user_test_pending": [],
                    "active_agents": {}
                }
            }
        
        try:
            with open(self.progress_file, 'r', encoding='utf-8') as f:
                return json.load(f)
        except (json.JSONDecodeError, IOError) as e:
            print(f"Error loading progress.json: {e}")
            raise
    
    def _save_progress(self, progress_data: Dict) -> bool:
        """progress.jsonファイルを保存"""
        try:
            # バックアップ作成
            backup_file = self.progress_file.with_suffix('.json.backup')
            if self.progress_file.exists():
                import shutil
                shutil.copy2(self.progress_file, backup_file)
            
            # 原子的書き込み（temp → rename）
            temp_file = self.progress_file.with_suffix('.json.tmp')
            with open(temp_file, 'w', encoding='utf-8') as f:
                json.dump(progress_data, f, indent=2, ensure_ascii=False)
            
            temp_file.replace(self.progress_file)
            return True
            
        except (IOError, OSError) as e:
            print(f"Error saving progress.json: {e}")
            return False
    
    def _update_progress_file(self, task_id: str, new_status: str, evidence: Dict, 
                             caller_context: Optional[str] = None) -> bool:
        """進捗ファイルの実際の更新処理"""
        progress = self._load_progress()
        
        # タスク情報更新
        if task_id not in progress["active_tasks"]:
            progress["active_tasks"][task_id] = {
                "status": "pending",
                "assignee": "",
                "worktree_path": f"worktrees/{task_id}",
                "branch_name": f"task-{task_id}",
                "dependencies": []
            }
        
        task_data = progress["active_tasks"][task_id]
        old_status = task_data.get("status", "unknown")
        
        # ステータス更新
        task_data["status"] = new_status
        task_data["last_updated"] = datetime.now().isoformat()
        task_data["evidence"] = evidence
        
        if caller_context:
            task_data["update_context"] = caller_context
        
        # 状態遷移ログ
        if "status_history" not in task_data:
            task_data["status_history"] = []
        
        task_data["status_history"].append({
            "from_status": old_status,
            "to_status": new_status,
            "timestamp": datetime.now().isoformat(),
            "evidence_summary": str(evidence)[:200] + "..." if len(str(evidence)) > 200 else str(evidence)
        })
        
        # 全体の進捗更新
        progress["last_updated"] = datetime.now().isoformat()
        
        return self._save_progress(progress)
    
    def get_readonly_progress(self) -> Dict:
        """読み取り専用の進捗データを取得"""
        return self._load_progress()
    
    def check_agent_permissions(self, agent_name: str) -> Dict[str, bool]:
        """エージェントの権限状況を確認（診断用）"""
        return {
            "can_update_progress": agent_name in self._authorized_callers,
            "can_update_agent_status": agent_name in self._authorized_callers,
            "can_add_user_tests": agent_name in self._authorized_callers,
            "current_caller_detected": self._get_caller_agent(),
            "is_main_agent": agent_name == "main-agent"
        }


# 外部エージェント向けの制限付きアクセス関数
def read_progress() -> Dict:
    """全エージェントが使用可能な読み取り専用アクセス"""
    manager = ProgressManager()
    return manager.get_readonly_progress()


def request_progress_update(task_id: str, new_status: str, evidence: Dict) -> Tuple[bool, str]:
    """
    エージェントからの進捗更新リクエスト（メインエージェント経由）
    
    注意：この関数は直接更新せず、メインエージェントにリクエストを送信する
    """
    print(f"[PROGRESS_UPDATE_REQUEST] Task {task_id}: {new_status}")
    print(f"[EVIDENCE] {evidence}")
    print(f"[NOTE] Direct progress updates are restricted. Main agent will process this request.")
    
    return False, "Progress update request logged. Awaiting main agent processing."


if __name__ == "__main__":
    # 診断モード
    print("ProgressManager Diagnostic Mode")
    print("=" * 50)
    
    manager = ProgressManager()
    
    # 現在の呼び出し元検出テスト
    caller = manager._get_caller_agent()
    print(f"Detected caller: {caller}")
    
    # 権限チェックテスト
    permissions = manager.check_agent_permissions(caller)
    print("Current permissions:")
    for perm, allowed in permissions.items():
        status = "[ALLOWED]" if allowed else "[DENIED]"
        print(f"  {perm}: {status}")
    
    # 進捗読み取りテスト
    try:
        progress = manager.get_readonly_progress()
        task_count = len(progress.get("active_tasks", {}))
        print(f"\nCurrent project status:")
        print(f"  Active tasks: {task_count}")
        print(f"  Last updated: {progress.get('last_updated', 'unknown')}")
    except Exception as e:
        print(f"Error reading progress: {e}")