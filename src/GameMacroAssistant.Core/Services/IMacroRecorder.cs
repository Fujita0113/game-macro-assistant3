using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface IMacroRecorder
{
    event EventHandler<StepRecordedEventArgs>? StepRecorded;
    event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;
    
    bool IsRecording { get; }
    
    Task StartRecordingAsync();
    
    Task StopRecordingAsync();
    
    void SetStopKey(int virtualKeyCode);
    
    Macro GetRecordedMacro();
    
    void ClearRecording();
}

public class StepRecordedEventArgs : EventArgs
{
    public Step Step { get; }
    
    public StepRecordedEventArgs(Step step)
    {
        Step = step;
    }
}

public class RecordingStateChangedEventArgs : EventArgs
{
    public bool IsRecording { get; }
    
    public RecordingStateChangedEventArgs(bool isRecording)
    {
        IsRecording = isRecording;
    }
}