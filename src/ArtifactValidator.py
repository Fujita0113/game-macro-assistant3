#!/usr/bin/env python3
"""
メインエージェント用成果物検証モジュール

タスク完了時の成果物の存在・品質を検証し、虚偽報告を防止する。
"""

import os
import json
import subprocess
import glob
from datetime import datetime
from typing import Dict, List, Tuple, Optional
from pathlib import Path


class ArtifactValidator:
    """成果物検証クラス"""
    
    def __init__(self, project_root: str = "."):
        self.project_root = Path(project_root).resolve()
        self.docs_path = self.project_root / "docs" / "pm" / "tasks"
    
    def validate_task_completion(self, task_id: str) -> Tuple[bool, str, Dict]:
        """
        タスク完了時の包括的検証
        
        Returns:
            (is_valid, error_message, validation_details)
        """
        details = {
            "task_id": task_id,
            "worktree_exists": False,
            "required_files_exist": False,
            "build_success": False,
            "test_results": {},
            "validation_timestamp": "",
            "errors": []
        }
        
        try:
            # 1. タスク仕様読み込み
            task_spec = self._load_task_specification(task_id)
            if not task_spec:
                details["errors"].append(f"Task specification not found: {task_id}")
                return False, f"Task specification not found for {task_id}", details
            
            # 2. Worktree存在確認
            worktree_path = self.project_root / "worktrees" / task_id
            if not worktree_path.exists():
                details["errors"].append(f"Worktree does not exist: {worktree_path}")
                return False, f"Worktree does not exist: {worktree_path}", details
            
            details["worktree_exists"] = True
            
            # 3. 必須ファイル存在確認
            required_files = task_spec.get("required_files", [])
            missing_files = []
            
            for required_file in required_files:
                file_path = worktree_path / required_file
                if not file_path.exists():
                    missing_files.append(required_file)
            
            if missing_files:
                details["errors"].append(f"Missing required files: {missing_files}")
                return False, f"Missing required files: {', '.join(missing_files)}", details
            
            details["required_files_exist"] = True
            
            # 4. ビルド確認
            build_success, build_message = self._validate_build(worktree_path)
            details["build_success"] = build_success
            
            if not build_success:
                details["errors"].append(f"Build failed: {build_message}")
                return False, f"Build failed: {build_message}", details
            
            # 5. テスト実行・確認
            test_results = self._validate_tests(worktree_path)
            details["test_results"] = test_results
            
            if test_results["failed_count"] > 0:
                details["errors"].append(f"Tests failed: {test_results['failed_count']} failures")
                return False, f"Tests failed: {test_results['failed_count']} failures", details
            
            # 6. カバレッジ確認（オプション）
            coverage_check = self._validate_coverage(worktree_path, task_spec.get("min_coverage", 80))
            if coverage_check["coverage_percent"] < task_spec.get("min_coverage", 80):
                details["errors"].append(f"Coverage below threshold: {coverage_check['coverage_percent']}%")
                # カバレッジ不足は警告のみ（必須ではない）
            
            details["validation_timestamp"] = datetime.now().isoformat()
            return True, "All validations passed", details
            
        except Exception as e:
            error_msg = f"Validation error: {e}"
            details["errors"].append(error_msg)
            return False, error_msg, details
    
    def _load_task_specification(self, task_id: str) -> Optional[Dict]:
        """タスク仕様ファイルを読み込み"""
        task_file = self.docs_path / f"{task_id}.md"
        
        if not task_file.exists():
            return None
        
        try:
            with open(task_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Markdown frontmatterからJSONを抽出（簡易実装）
            if '```json' in content:
                json_start = content.find('```json') + 7
                json_end = content.find('```', json_start)
                json_content = content[json_start:json_end].strip()
                return json.loads(json_content)
            
            # デフォルト仕様
            return {
                "required_files": [f"src/**/{task_id.replace('T-', '').replace('-', '')}*.cs"],
                "min_coverage": 80,
                "build_command": "dotnet build",
                "test_command": "dotnet test"
            }
            
        except Exception as e:
            print(f"Error loading task spec {task_id}: {e}")
            return None
    
    def _validate_build(self, worktree_path: Path) -> Tuple[bool, str]:
        """ビルド検証"""
        try:
            # .NET プロジェクトのビルド
            result = subprocess.run(
                ["dotnet", "build", "--configuration", "Release"],
                cwd=worktree_path,
                capture_output=True,
                text=True,
                timeout=300  # 5分タイムアウト
            )
            
            if result.returncode == 0:
                return True, "Build succeeded"
            else:
                return False, result.stderr or result.stdout
                
        except subprocess.TimeoutExpired:
            return False, "Build timeout (5 minutes)"
        except FileNotFoundError:
            return False, "dotnet command not found"
        except Exception as e:
            return False, f"Build error: {e}"
    
    def _validate_tests(self, worktree_path: Path) -> Dict:
        """テスト実行・結果検証"""
        test_results = {
            "total_count": 0,
            "passed_count": 0,
            "failed_count": 0,
            "skipped_count": 0,
            "execution_time": "0s",
            "details": []
        }
        
        try:
            # .NET テスト実行
            result = subprocess.run(
                ["dotnet", "test", "--logger", "trx", "--collect:XPlat Code Coverage"],
                cwd=worktree_path,
                capture_output=True,
                text=True,
                timeout=600  # 10分タイムアウト
            )
            
            output = result.stdout + result.stderr
            
            # 結果パース（簡易実装）
            if "Total tests:" in output:
                for line in output.split('\n'):
                    if "Total tests:" in line:
                        # 例: "Total tests: 10. Passed: 8. Failed: 2. Skipped: 0."
                        parts = line.split('.')
                        for part in parts:
                            if "Total tests:" in part:
                                test_results["total_count"] = int(part.split(':')[1].strip())
                            elif "Passed:" in part:
                                test_results["passed_count"] = int(part.split(':')[1].strip())
                            elif "Failed:" in part:
                                test_results["failed_count"] = int(part.split(':')[1].strip())
                            elif "Skipped:" in part:
                                test_results["skipped_count"] = int(part.split(':')[1].strip())
            
            test_results["success"] = result.returncode == 0
            return test_results
            
        except subprocess.TimeoutExpired:
            test_results["details"].append("Test execution timeout (10 minutes)")
            test_results["failed_count"] = 1
            return test_results
        except Exception as e:
            test_results["details"].append(f"Test execution error: {e}")
            test_results["failed_count"] = 1
            return test_results
    
    def _validate_coverage(self, worktree_path: Path, min_coverage: float) -> Dict:
        """コードカバレッジ検証"""
        coverage_info = {
            "coverage_percent": 0.0,
            "covered_lines": 0,
            "total_lines": 0,
            "report_path": "",
            "meets_threshold": False
        }
        
        try:
            # coverletで生成されたカバレッジファイルを探す
            coverage_files = list(worktree_path.glob("**/coverage.cobertura.xml"))
            if not coverage_files:
                coverage_files = list(worktree_path.glob("**/TestResults/**/coverage.cobertura.xml"))
            
            if not coverage_files:
                coverage_info["coverage_percent"] = 0.0
                return coverage_info
            
            # 最新のカバレッジファイルを使用
            coverage_file = max(coverage_files, key=lambda f: f.stat().st_mtime)
            coverage_info["report_path"] = str(coverage_file)
            
            # XML解析してカバレッジ取得（簡易実装）
            with open(coverage_file, 'r', encoding='utf-8') as f:
                content = f.read()
                
                # line-rate属性から取得
                import re
                match = re.search(r'line-rate="([\d.]+)"', content)
                if match:
                    coverage_info["coverage_percent"] = float(match.group(1)) * 100
                    coverage_info["meets_threshold"] = coverage_info["coverage_percent"] >= min_coverage
            
            return coverage_info
            
        except Exception as e:
            print(f"Coverage validation error: {e}")
            return coverage_info
    
    def validate_agent_evidence(self, evidence: Dict) -> Tuple[bool, List[str]]:
        """エージェント報告証跡の検証"""
        errors = []
        
        # ファイル存在確認
        for field in ["files", "reviewed_files", "test_file_path"]:
            if field in evidence:
                file_paths = evidence[field]
                if isinstance(file_paths, str):
                    file_paths = [file_paths]
                
                for file_path in file_paths:
                    full_path = self.project_root / file_path
                    if not full_path.exists():
                        errors.append(f"Evidence file does not exist: {file_path}")
        
        # ビルドステータス確認
        if "build_status" in evidence:
            if evidence["build_status"] not in ["success", "failed"]:
                errors.append(f"Invalid build_status: {evidence['build_status']}")
        
        # カバレッジ妥当性確認
        if "test_coverage" in evidence:
            coverage_str = evidence["test_coverage"]
            try:
                if isinstance(coverage_str, str) and coverage_str.endswith('%'):
                    coverage_val = float(coverage_str.rstrip('%'))
                    if coverage_val < 0 or coverage_val > 100:
                        errors.append(f"Invalid coverage value: {coverage_str}")
            except ValueError:
                errors.append(f"Invalid coverage format: {coverage_str}")
        
        return len(errors) == 0, errors


def validate_task_artifacts(task_id: str) -> Tuple[bool, str]:
    """
    外部呼び出し用のタスク検証関数
    
    Returns:
        (is_valid, message)
    """
    validator = ArtifactValidator()
    is_valid, message, details = validator.validate_task_completion(task_id)
    
    if is_valid:
        coverage_pct = details.get("test_results", {}).get("coverage_percent", "unknown")
        test_count = details.get("test_results", {}).get("total_count", 0)
        return True, f"Task {task_id} validated: {test_count} tests passed, coverage: {coverage_pct}%"
    else:
        return False, message


if __name__ == "__main__":
    # テストケース実行
    import sys
    
    if len(sys.argv) > 1:
        task_id = sys.argv[1]
        is_valid, message = validate_task_artifacts(task_id)
        print(f"Task {task_id}: {'✅ VALID' if is_valid else '❌ INVALID'}")
        print(f"Message: {message}")
    else:
        print("Usage: python ArtifactValidator.py <TaskID>")
        print("Example: python ArtifactValidator.py T-009")