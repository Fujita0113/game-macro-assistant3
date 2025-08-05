#!/usr/bin/env python3
"""
GameMacroAssistant Progress Visualizer
プロジェクト進捗の視覚的表示システム
"""

import json
import os
from datetime import datetime, timezone, timedelta
from typing import Dict, List, Optional
import sys

class ProgressVisualizer:
    def __init__(self, progress_file_path: str = ".claude/progress.json"):
        self.progress_file = progress_file_path
        self.progress_data = None
        self.load_progress()
    
    def load_progress(self) -> bool:
        """progress.jsonファイルを読み込む"""
        try:
            if os.path.exists(self.progress_file):
                with open(self.progress_file, 'r', encoding='utf-8') as f:
                    self.progress_data = json.load(f)
                return True
            else:
                print(f"ERROR: {self.progress_file} not found")
                return False
        except json.JSONDecodeError as e:
            print(f"ERROR: Invalid JSON format: {e}")
            return False
        except Exception as e:
            print(f"ERROR: File read error: {e}")
            return False
    
    def create_progress_bar(self, current: int, total: int, width: int = 20) -> str:
        """Generate progress bar"""
        if total == 0:
            return "-" * width + " 0%"
        
        progress = current / total
        filled_width = int(width * progress)
        bar = "#" * filled_width + "-" * (width - filled_width)
        percentage = progress * 100
        return f"{bar} {percentage:.1f}%"
    
    def estimate_completion_time(self) -> Optional[str]:
        """完了予想時間を計算"""
        if not self.progress_data:
            return None
        
        metrics = self.progress_data.get("project_metrics", {})
        total_tasks = metrics.get("total_tasks_planned", 0)
        completed_tasks = metrics.get("completed_tasks", 0)
        velocity = metrics.get("sprint_velocity", 1)
        
        if total_tasks == 0 or velocity == 0:
            return None
        
        remaining_tasks = total_tasks - completed_tasks
        if remaining_tasks <= 0:
            return "[COMPLETED]"
        
        # 1スプリントあたりの平均タスク数から予想
        sprints_remaining = max(1, remaining_tasks / velocity)
        days_remaining = sprints_remaining * 7  # 1スプリント = 7日と仮定
        
        estimated_completion = datetime.now() + timedelta(days=days_remaining)
        return estimated_completion.strftime("%Y-%m-%d %H:%M")
    
    def get_task_status_icon(self, status: str) -> str:
        """Get task status icon"""
        icons = {
            "completed": "[DONE]",
            "in_progress": "[WORK]",
            "pending": "[WAIT]",
            "blocked": "[STOP]",
            "reviewing": "[REVW]",
            "testing": "[TEST]"
        }
        return icons.get(status, "[????]")
    
    def show_sprint_overview(self):
        """スプリント概要を表示"""
        if not self.progress_data:
            return
        
        current_sprint = self.progress_data.get("current_sprint", "unknown")
        current_phase = self.progress_data.get("current_phase", "unknown")
        
        print(f"🎯 スプリント: {current_sprint}")
        print(f"📊 現在フェーズ: {current_phase}")
        
        # プロジェクト全体の進捗
        metrics = self.progress_data.get("project_metrics", {})
        total_tasks = metrics.get("total_tasks_planned", 0)
        completed_tasks = metrics.get("completed_tasks", 0)
        in_progress_tasks = metrics.get("in_progress_tasks", 0)
        
        progress_bar = self.create_progress_bar(completed_tasks, total_tasks)
        print(f"📈 全体進捗: {progress_bar} ({completed_tasks}/{total_tasks})")
        
        completion_estimate = self.estimate_completion_time()
        if completion_estimate:
            print(f"⏰ 完了予想: {completion_estimate}")
        
        print()
    
    def show_active_tasks(self):
        """アクティブタスクを表示"""
        if not self.progress_data:
            return
        
        print("📋 タスク状況:")
        print("-" * 50)
        
        # 完了タスク
        active_tasks = self.progress_data.get("active_tasks", {})
        completed_count = 0
        for task_id, task_data in active_tasks.items():
            if task_data.get("status") == "completed":
                completed_count += 1
                icon = self.get_task_status_icon("completed")
                description = task_data.get("description", "説明なし")
                assignee = task_data.get("assignee", "未割当")
                print(f"  {icon} {task_id}: {description} ({assignee})")
        
        # 進行中タスク
        working_tasks = self.progress_data.get("current_working_tasks", {})
        for task_id, task_data in working_tasks.items():
            icon = self.get_task_status_icon("in_progress")
            description = task_data.get("description", "説明なし")
            assignee = task_data.get("assignee", "未割当")
            progress = task_data.get("progress_details", {}).get("implementation_progress", "不明")
            print(f"  {icon} {task_id}: {description} ({assignee}) - {progress}")
            
            # 中断情報がある場合
            if "interruption_point" in task_data:
                interruption = task_data["interruption_point"]
                reason = interruption.get("reason", "不明")
                next_action = interruption.get("next_action", "不明")
                print(f"      ⚠️  中断: {reason} → 次: {next_action}")
        
        # 待機中タスク
        next_tasks = self.progress_data.get("next_available_tasks", [])
        if next_tasks:
            print(f"  ⏳ 待機中: {', '.join(next_tasks[:3])}")
            if len(next_tasks) > 3:
                print(f"      他{len(next_tasks) - 3}個...")
        
        print()
    
    def show_agent_status(self):
        """エージェント稼働状況を表示"""
        if not self.progress_data:
            return
        
        workflow_state = self.progress_data.get("workflow_state", {})
        active_agents = workflow_state.get("active_agents", {})
        
        if not active_agents:
            return
        
        print("🤖 エージェント状況:")
        print("-" * 30)
        
        for agent_name, agent_data in active_agents.items():
            status = agent_data.get("status", "unknown")
            current_task = agent_data.get("current_task")
            last_heartbeat = agent_data.get("last_heartbeat", "不明")
            
            status_icon = "🟢" if status == "working" else "⚪" if status == "idle" else "🔴"
            
            print(f"  {status_icon} {agent_name}: {status}")
            if current_task:
                print(f"      📋 作業中: {current_task}")
            if last_heartbeat != "不明":
                try:
                    heartbeat_time = datetime.fromisoformat(last_heartbeat.replace('Z', '+00:00'))
                    time_diff = datetime.now(timezone.utc) - heartbeat_time
                    if time_diff.total_seconds() < 300:  # 5分以内
                        print(f"      💓 最終確認: {int(time_diff.total_seconds())}秒前")
                    else:
                        print(f"      ⚠️ 最終確認: {last_heartbeat}")
                except:
                    print(f"      📅 最終確認: {last_heartbeat}")
        
        print()
    
    def show_test_coverage(self):
        """テストカバレッジ情報を表示"""
        if not self.progress_data:
            return
        
        metrics = self.progress_data.get("project_metrics", {})
        coverage = metrics.get("test_coverage")
        
        if coverage:
            try:
                coverage_pct = float(coverage.replace('%', ''))
                coverage_bar = self.create_progress_bar(int(coverage_pct), 100, width=15)
                
                # カバレッジ品質の判定
                if coverage_pct >= 80:
                    quality_icon = "✅"
                    quality_text = "良好"
                elif coverage_pct >= 60:
                    quality_icon = "⚠️"
                    quality_text = "改善推奨"
                else:
                    quality_icon = "❌"
                    quality_text = "要改善"
                
                print(f"🧪 テストカバレッジ: {coverage_bar} ({coverage}) {quality_icon} {quality_text}")
                print()
            except:
                print(f"🧪 テストカバレッジ: {coverage}")
                print()
    
    def show_timeline(self):
        """最近の活動タイムラインを表示"""
        if not self.progress_data:
            return
        
        print("📅 最近の活動:")
        print("-" * 40)
        
        # 最後の更新時刻
        last_updated = self.progress_data.get("last_updated")
        if last_updated:
            print(f"  📝 最終更新: {last_updated}")
        
        # 完了したスプリント
        completed_sprints = self.progress_data.get("completed_sprints", [])
        if completed_sprints:
            print(f"  ✅ 完了スプリント: {', '.join(completed_sprints)}")
        
        # セッション情報
        session_context = self.progress_data.get("session_context", {})
        if session_context:
            last_session = session_context.get("last_session_end")
            interruption_cause = session_context.get("interruption_cause")
            
            if last_session:
                print(f"  🔄 前回セッション終了: {last_session}")
            if interruption_cause:
                print(f"  ⚠️ 中断原因: {interruption_cause}")
        
        print()
    
    def show_dashboard(self):
        """Display complete dashboard"""
        if not self.progress_data:
            print("ERROR: Failed to load progress.json")
            return
        
        print("GameMacroAssistant Development Dashboard")
        print("=" * 60)
        print()
        
        # Basic project info
        print(f"Project: {self.progress_data.get('project_id', 'Unknown')}")
        print(f"Current Sprint: {self.progress_data.get('current_sprint', 'Unknown')}")
        print(f"Current Phase: {self.progress_data.get('current_phase', 'Unknown')}")
        print()
        
        # Progress
        metrics = self.progress_data.get("project_metrics", {})
        total_tasks = metrics.get("total_tasks_planned", 0)
        completed_tasks = metrics.get("completed_tasks", 0)
        in_progress_tasks = metrics.get("in_progress_tasks", 0)
        
        if total_tasks > 0:
            progress_bar = self.create_progress_bar(completed_tasks, total_tasks)
            print(f"Overall Progress: {progress_bar} ({completed_tasks}/{total_tasks})")
        
        print(f"In Progress: {in_progress_tasks}")
        print()
        
        # Interrupted work
        working_tasks = self.progress_data.get("current_working_tasks", {})
        if working_tasks:
            print("INTERRUPTED WORK:")
            for task_id, task_data in working_tasks.items():
                desc = task_data.get("description", "No description")
                progress = task_data.get("progress_details", {}).get("implementation_progress", "Unknown")
                print(f"  {task_id}: {desc} ({progress})")
                
                interruption = task_data.get("interruption_point", {})
                if interruption:
                    reason = interruption.get("reason", "Unknown")
                    next_action = interruption.get("next_action", "Unknown")
                    print(f"    Reason: {reason}")
                    print(f"    Next: {next_action}")
            print()
        
        # Test coverage
        coverage = metrics.get("test_coverage", "Unknown")
        print(f"Test Coverage: {coverage}")
        
        print()
        print("Dashboard Complete")

def main():
    """メイン実行関数"""
    visualizer = ProgressVisualizer()
    
    # コマンドライン引数による機能選択
    if len(sys.argv) > 1:
        command = sys.argv[1].lower()
        if command == "overview":
            visualizer.show_sprint_overview()
        elif command == "tasks":
            visualizer.show_active_tasks()
        elif command == "agents":
            visualizer.show_agent_status()
        elif command == "timeline":
            visualizer.show_timeline()
        else:
            visualizer.show_dashboard()
    else:
        visualizer.show_dashboard()

if __name__ == "__main__":
    main()