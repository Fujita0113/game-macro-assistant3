#!/usr/bin/env python3
"""
エージェント完了シグナルの解析・検証モジュール

完了シグナルに成果物証跡を義務化し、信頼性を担保する。
"""

import json
import re
from typing import Dict, List, Tuple, Optional
from dataclasses import dataclass
from pathlib import Path


@dataclass
class CompletionSignal:
    """完了シグナルの構造化データ"""
    signal_type: str
    task_id: str
    evidence: Dict
    raw_signal: str
    
    def __post_init__(self):
        if not self.evidence:
            self.evidence = {}


class SignalParser:
    """完了シグナルのパーサー・バリデーター"""
    
    # 証跡要件定義：タスク種別ごとの必須フィールド
    EVIDENCE_REQUIREMENTS = {
        "implementation": {
            "required": ["files", "build_status", "test_coverage"],
            "optional": ["tests_passing", "tests_total", "warnings_count"]
        },
        "review": {
            "required": ["reviewed_files", "coverage_percent", "issues_found"],
            "optional": ["static_analysis_result", "build_time_ms"]
        },
        "testdoc": {
            "required": ["test_file_path", "test_count", "estimated_minutes"],
            "optional": ["worktree_path", "setup_requirements"]
        },
        "integration": {
            "required": ["merged_files", "integration_tests_status", "conflicts_resolved"],
            "optional": ["merge_commit_hash", "rollback_available"]
        }
    }
    
    # シグナルタイプマッピング
    SIGNAL_TYPE_MAP = {
        "##DEV_DONE##": "implementation",
        "##REVIEW_PASS##": "review", 
        "##REVIEW_FAIL##": "review",
        "##TESTDOC_COMPLETE##": "testdoc",
        "##INTEGRATION_COMPLETE##": "integration"
    }
    
    def parse_signal(self, raw_signal: str) -> CompletionSignal:
        """
        生シグナルを解析して構造化データに変換
        
        Format: ##SIGNAL_TYPE##|evidence:{'key':'value',...}
        または旧形式: ##SIGNAL_TYPE##
        """
        raw_signal = raw_signal.strip()
        
        # 新形式（証跡付き）の解析
        if "|evidence:" in raw_signal:
            parts = raw_signal.split("|evidence:", 1)
            signal_part = parts[0]
            evidence_part = parts[1]
            
            try:
                evidence = json.loads(evidence_part)
            except json.JSONDecodeError as e:
                raise ValueError(f"Invalid evidence JSON: {evidence_part}. Error: {e}")
        else:
            # 旧形式（証跡なし）
            signal_part = raw_signal
            evidence = {}
        
        # TaskID抽出（例：##DEV_DONE##からは抽出不可、evidenceから取得）
        task_id = evidence.get("task_id", "")
        
        return CompletionSignal(
            signal_type=signal_part,
            task_id=task_id,
            evidence=evidence,
            raw_signal=raw_signal
        )
    
    def validate_evidence(self, signal: CompletionSignal) -> Tuple[bool, List[str]]:
        """
        証跡の妥当性検証
        
        Returns:
            (is_valid, error_messages)
        """
        signal_type = signal.signal_type
        evidence = signal.evidence
        
        # 旧形式シグナル（証跡なし）は検証失敗
        if not evidence:
            return False, [f"Evidence required for {signal_type}. Use new format: {signal_type}|evidence:{{...}}"]
        
        # 対応するタスク種別を特定
        task_type = self.SIGNAL_TYPE_MAP.get(signal_type)
        if not task_type:
            return False, [f"Unknown signal type: {signal_type}"]
        
        # 必須フィールドの検証
        requirements = self.EVIDENCE_REQUIREMENTS[task_type]
        required_fields = requirements["required"]
        
        errors = []
        for field in required_fields:
            if field not in evidence:
                errors.append(f"Missing required evidence field: {field}")
            elif not evidence[field]:  # 空文字列・None・空配列チェック
                errors.append(f"Empty required evidence field: {field}")
        
        # ファイルパス存在確認（files, reviewed_files, test_file_path等）
        file_fields = ["files", "reviewed_files", "test_file_path", "merged_files"]
        for field in file_fields:
            if field in evidence:
                file_paths = evidence[field]
                if isinstance(file_paths, str):
                    file_paths = [file_paths]
                
                for file_path in file_paths:
                    if not Path(file_path).exists():
                        errors.append(f"Evidence file does not exist: {file_path}")
        
        return len(errors) == 0, errors
    
    def extract_task_id_from_evidence(self, evidence: Dict) -> Optional[str]:
        """証跡からTaskIDを抽出"""
        # 直接指定
        if "task_id" in evidence:
            return evidence["task_id"]
        
        # ファイルパスから推定 (worktrees/T-009/... -> T-009)
        for field in ["files", "reviewed_files", "test_file_path"]:
            if field in evidence:
                paths = evidence[field] if isinstance(evidence[field], list) else [evidence[field]]
                for path in paths:
                    match = re.search(r'worktrees/(T-\d+)', path)
                    if match:
                        return match.group(1)
        
        return None


def validate_completion_signal(raw_signal: str) -> Tuple[bool, str, Optional[CompletionSignal]]:
    """
    完了シグナルの包括的検証（外部呼び出し用）
    
    Returns:
        (is_valid, message, parsed_signal_or_none)
    """
    parser = SignalParser()
    
    try:
        signal = parser.parse_signal(raw_signal)
        is_valid, errors = parser.validate_evidence(signal)
        
        if not is_valid:
            return False, "; ".join(errors), None
        
        return True, "Signal validation passed", signal
        
    except Exception as e:
        return False, f"Signal parsing failed: {e}", None


if __name__ == "__main__":
    # テストケース実行
    test_signals = [
        "##DEV_DONE##",  # 旧形式（失敗予定）
        """##DEV_DONE##|evidence:{"task_id":"T-009","files":["src/Service.cs","tests/ServiceTests.cs"],"build_status":"success","test_coverage":"85%"}""",  # 新形式
        """##REVIEW_PASS##|evidence:{"task_id":"T-009","reviewed_files":["src/Service.cs"],"coverage_percent":"85","issues_found":"0"}"""
    ]
    
    for signal in test_signals:
        is_valid, message, parsed = validate_completion_signal(signal)
        print(f"Signal: {signal[:50]}...")
        print(f"Valid: {is_valid}, Message: {message}")
        if parsed:
            print(f"TaskID: {parsed.task_id}, Evidence keys: {list(parsed.evidence.keys())}")
        print("-" * 80)