   M S B A M L   `   `   `  ����  GameMacroAssistant.Wpf+    %GameMacroAssistant.Wpf.Views.MainView   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Game Macro Assistant��5   	   $	��600��6&   $	��800��63   $	��400��5   	   $	��600��6   .��5      �  � RowDefinitions 5   
   �� 5	      $
��Auto �6   �� 5
      $��* �6   �� 5      $
��Auto �6   .+�5   
   c� $��0��6   $��0,0,0,5q�6   .@�5      ��    PrimaryButton#��[  5      ��5      �� *StartRecordingCommand+.��5      ��  �� Orientation$ 
Horizontal=�6   .+�5      �� $	��●��6    
���  ��$��16(�6:   $��0,0,5,0q�6H   �� 5      $��Record��6    5      5      �� 5      $��5,0,0,0q�5      ��5      �� *StopRecordingCommand+.��5      �� $ 
Horizontal=�6   .+�5      �� $	��■��6    $��12(�6)   $��0,0,5,0q�67   �� 5      $
��Stop��6    5      5      �� 5       $
��10,0q�6   �� 5"      #��[  5#      ��5"      �� *ExecuteMacroCommand+.��5$      �� $ 
Horizontal=�6   .+�5%      �� $	��▶��6    $��12(�6)   $��0,0,5,0q�67   �� 5&      $
��Play��6    5'      5(      �� 5*      $
��10,0q�6   �� 5,      ��6   �� *EditMacroCommand+.��5-      �� $ 
Horizontal=�6   .+�5.      �� $	��✏��6    $��12(�6)   $��0,0,5,0q�67   �� 5/      $
��Edit��6    50      51      �� 53      $��5,0,0,0q�6<   ��6   �� *DeleteMacroCommand+.��54      �� $ 
Horizontal=�6   .+�55      �� $
��🗑��6    $��12(�6*   $��0,0,5,0q�68   �� 56      $��Delete��6    57      58      �� 5:      $
��10,0q�6   �� 5<      ��6   �� *LoadMacroFromFileCommand+.��5=      �� $ 
Horizontal=�6   .+�5>      �� $
��📁��6    $��12(�6*   $��0,0,5,0q�68   �� 5?      $
��Load��6    5@      5A      �� 5C      $��5,0,0,0q�6@   ��6   �� *SaveMacroToFileCommand+.��5D      �� $ 
Horizontal=�6   .+�5E      �� $
��💾��6    $��12(�6*   $��0,0,5,0q�68   �� 5F      $
��Save��6    5G      5H      �� 5J      $��5,0,0,0q�6A   ��6   �� *RefreshMacroListCommand+.��5K      �� $ 
Horizontal=�6   .+�5L      �� $
��🔄��6    $��12(�6*   $��0,0,5,0q�68   �� 5M      $��Refresh��6    5N      5O      �� 5Q      $
��10,0q�6   �� 5S      ��6   �� *OpenSettingsCommand+.��5T      �� $ 
Horizontal=�6   .+�5U      �� $	��⚙��6    $��12(�6)   $��0,0,5,0q�67   �� 5V      $��Settings��6    5W      5X      5Y      � 5\   
   $��1��6   $��10q�6    � ColumnDefinitions 5]      �� 5^      $��* �6#   �� 5_      $��5 �6#   �� 5`      $	��300 �6#   .+�5d      �� $��Macros��6   $��0��6'   .��5e      &   System.Windows.Controls.DataGrid     AutoGenerateColumns$ False��5g         CanUserAddRows$ False��5h         GridLinesVisibility$ 
Horizontal=�5i         HeadersVisibility$ Column=�5j      ��5e      �� *Macros+ �� SelectedItem 5f      �� *SelectedMacro+	   Columns	 5k      0  *System.Windows.Controls.DataGridTextColumn  5l      ,  &System.Windows.Controls.DataGridColumn
   Header$

 Name��6-      Width5  /System.Windows.Controls.DataGridLengthConverter$	 200 5n   +   1  +System.Windows.Controls.DataGridBoundColumn   Binding 5m   +   �� *Name+5n   +     5o      $
 Description��6-   $ * 5q   +    5p   +   �� *Description+5q   +     5r      $
 Steps��6-   $ 60 5t   +    5s   +   �� *Steps.Count+5t   +     5u      $
 Created��6-   $ 80 5w   +    5v   +   �� *	CreatedAt+ �� StringFormat$ {0:MM/dd/yyyy}��5w   +   5y      5z      �� 5|      $��1��6   $��Stretch=�6+   �� 5      $��
Properties��6   $��2��6+   .��5�      �� .��5�      �� $��5q�6!   .+�5�      �� $��Macro Details��6$   $
��Bold"�69   $��0,0,0,10q�6K   �� 5�      $��Name:��6$   $��SemiBold"�61   �� 5�      $��0,2,0,10q�5�   "   ��5�   "   �� *SelectedMacro.Name+ �� UpdateSourceTrigger$ PropertyChanged=�5�   "   �� 5�      $��Description:��6$   $��SemiBold"�68   �� 5�      $��0,2,0,10q�5�   "    �� TextWrapping$
 Wrap=�5�   "   $��60��5�   "   ��5�   "   �� *SelectedMacro.Description+$ PropertyChanged=�5�   "   �� 5�      $��Settings��6$   $
��Bold"�64   $��	0,20,0,10q�6F   �� 5�      $��Global Hotkey:��6$   $��SemiBold"�6:   �� 5�      $��0,2,0,10q�5�   "   ��5�   "   �� *%#SelectedMacro.Settings.GlobalHotkey+$ PropertyChanged=�5�   "   �� 5�      $��Image Match Threshold:��6$   $��SemiBold"�6B   �� 5�       � Minimum$	 0.5Q�5�   !    � Maximum$	 1.0Q�6/    �� TickFrequency$
 0.05Q�5�   !    �� IsSnapToTickEnabled$
 True��5�   !   $��0,2,0,5q�5�   !    � Value 5�   !   �� *,*SelectedMacro.Settings.ImageMatchThreshold+5�   !   �� 5�      $��Center=�5�   $   $��0,0,0,10q�5�   $   ��5�   $   �� *,*SelectedMacro.Settings.ImageMatchThreshold+$ {0:P0}��5�   $   �� 5�      $��Timeout (ms):��6$   $��SemiBold"�69   �� 5�      $��0,2,0,10q�5�   "   ��5�   "   �� *" SelectedMacro.Settings.TimeoutMs+$ PropertyChanged=�5�   "   5�      5�      5�      5�      �� 5�   
   $��2��6   .@�5�      �� .��5�      �� ��6   �� *StatusMessage+5�      �� 5�      $��Right=�6   .��5�      �� $ 
Horizontal=�6   .+�5�      �� $��
Recording:��6    $��0,0,5,0q�62   �� 5�      $
��Bold"�6=   ��6    �� *IsRecording+6=   �� 5�      $
��10,0q�6    �� 5�      $��
Executing:��6    $��0,0,5,0q�62   �� 5�      $
��Bold"�6=   ��6    �� *IsExecuting+6=   5�      5�      5�      5�      5�      