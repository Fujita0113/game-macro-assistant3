   M S B A M L   `   `   `  ����   GameMacroAssistant.Wpf+    %GameMacroAssistant.Wpf.Views.MainView   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml       � Title$  Game Macro Assistant��$	��600��$	��800��$	��400��$	��600��.���  � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+�c� $��0��$��0,0,0,5q�.@���    PrimaryButton#��[  ���� *StartRecordingCommand+.����  �� Orientation$ 
Horizontal=�.+��� $	��●��
���  ��$��16(�$��0,0,5,0q��� $��Record���� $��5,0,0,0q����� *StopRecordingCommand+.���� $ 
Horizontal=�.+��� $	��■��$��12(�$��0,0,5,0q��� $
��Stop���� $
��10,0q��� #��[  ���� *ExecuteMacroCommand+.���� $ 
Horizontal=�.+��� $	��▶��$��12(�$��0,0,5,0q��� $
��Play���� $
��10,0q��� ���� *EditMacroCommand+.���� $ 
Horizontal=�.+��� $	��✏��$��12(�$��0,0,5,0q��� $
��Edit���� $��5,0,0,0q����� *DeleteMacroCommand+.���� $ 
Horizontal=�.+��� $
��🗑��$��12(�$��0,0,5,0q��� $��Delete���� $
��10,0q��� ���� *LoadMacroFromFileCommand+.���� $ 
Horizontal=�.+��� $
��📁��$��12(�$��0,0,5,0q��� $
��Load���� $��5,0,0,0q����� *SaveMacroToFileCommand+.���� $ 
Horizontal=�.+��� $
��💾��$��12(�$��0,0,5,0q��� $
��Save���� $��5,0,0,0q����� *RefreshMacroListCommand+.���� $ 
Horizontal=�.+��� $
��🔄��$��12(�$��0,0,5,0q��� $��Refresh���� $
��10,0q��� ���� *OpenSettingsCommand+.���� $ 
Horizontal=�.+��� $	��⚙��$��12(�$��0,0,5,0q��� $��Settings��� $��1��$��10q� � ColumnDefinitions �� $��* ��� $��5 ��� $	��300 �.+��� $��Macros��$��0��.��&   System.Windows.Controls.DataGrid     AutoGenerateColumns$ False��   CanUserAddRows$ False��   GridLinesVisibility$ 
Horizontal=�   HeadersVisibility$ Column=����� *Macros+ �� SelectedItem �� *SelectedMacro+	   Columns	 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn
   Header$

 Name��   Width5  /System.Windows.Controls.DataGridLengthConverter$	 200 1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *Name+  $
 Description��$ *  �� *Description+  $
 Steps��$ 60  �� *Steps.Count+  $
 Created��$ 80  �� *	CreatedAt+ �� StringFormat$ {0:MM/dd/yyyy}���� $��1��$��Stretch=��� $��
Properties��$��2��.���� .���� $��5q�.+��� $��Macro Details��$
��Bold"�$��0,0,0,10q��� $��Name:��$��SemiBold"��� $��0,2,0,10q����� *SelectedMacro.Name+ �� UpdateSourceTrigger$ PropertyChanged=��� $��Description:��$��SemiBold"��� $��0,2,0,10q� �� TextWrapping$
 Wrap=�$��60������ *SelectedMacro.Description+$ PropertyChanged=��� $��Settings��$
��Bold"�$��	0,20,0,10q��� $��Global Hotkey:��$��SemiBold"��� $��0,2,0,10q����� *%#SelectedMacro.Settings.GlobalHotkey+$ PropertyChanged=��� $��Image Match Threshold:��$��SemiBold"���  � Minimum$	 0.5Q� � Maximum$	 1.0Q� �� TickFrequency$
 0.05Q� �� IsSnapToTickEnabled$
 True��$��0,2,0,5q� � Value �� *,*SelectedMacro.Settings.ImageMatchThreshold+�� $��Center=�$��0,0,0,10q����� *,*SelectedMacro.Settings.ImageMatchThreshold+$ {0:P0}���� $��Timeout (ms):��$��SemiBold"��� $��0,2,0,10q����� *" SelectedMacro.Settings.TimeoutMs+$ PropertyChanged=��� $��2��.@��� .���� ���� *StatusMessage+�� $��Right=�.���� $ 
Horizontal=�.+��� $��
Recording:��$��0,0,5,0q��� $
��Bold"����� *IsRecording+�� $
��10,0q��� $��
Executing:��$��0,0,5,0q��� $
��Bold"����� *IsExecuting+