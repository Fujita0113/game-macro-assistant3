   M S B A M L   `   `   `  ����   GameMacroAssistant.Wpf-    'GameMacroAssistant.Wpf.Views.EditorView   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml       � Title$  Macro Editor��$	��700��$
��1000��$	��500��$	��800��.���  � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+�c� $��0��$��0,0,0,5q�.@��� ���� *UndoCommand+.����  �� Orientation$ 
Horizontal=�.+��� $	��↶��$��14(�$��0,0,5,0q��� $
��Undo���� $��5,0,0,0q����� *RedoCommand+.���� $ 
Horizontal=�.+��� $	��↷��$��14(�$��0,0,5,0q��� $
��Redo���� $
��10,0q��� ���� *DeleteStepCommand+.���� $ 
Horizontal=�.+��� $
��🗑��$��12(�$��0,0,5,0q��� $��Delete���� $��5,0,0,0q����� *DuplicateStepCommand+.���� $ 
Horizontal=�.+��� $
��📋��$��12(�$��0,0,5,0q��� $��	Duplicate���� $
��10,0q��� ���� *MoveStepUpCommand+.���� $ 
Horizontal=�.+��� $	��↑��$��12(�$��0,0,5,0q��� $��Move Up���� $��5,0,0,0q����� *MoveStepDownCommand+.���� $ 
Horizontal=�.+��� $	��↓��$��12(�$��0,0,5,0q��� $��	Move Down���� $
��10,0q���    PrimaryButton#��[  ���� *SaveMacroCommand+.���� $ 
Horizontal=�.+��� $
��💾��$��12(�$��0,0,5,0q��� $
��Save��� $��1��$��10q� � ColumnDefinitions �� $��* ��� $��5 ��� $	��350 �.+��� $��Macro Steps��$��0��.����  J� 	AllowDrop$
 True��$��Disabled=����� *Steps+ �� SelectedItem �� *SelectedStep+���� .R��� ���#E0E0E0$��1q� �� CornerRadius$ 4�� �� Padding$ 10q�$	��0,2q�
�������._��  �� $
��Auto ��� $��* ��� $
��Auto �.+��� $��0��$
��Bold"�$��Center=�$��30������ *
Step.Order+�� $��1��$
��10,0q�.+��� $��SemiBold"����� *DisplayName+�� 
�������$
��Wrap=����� *Description+�� $��2��$
��Edit������ *'%DataContext.EditStepParametersCommand+ �� RelativeSource �� 	 �� AncestorType	 ListBox���� *Step+�� $��1��$��Stretch=��� $��Step Properties��$��2��.���� .���� $��5q�.+��� $��General Settings��$
��Bold"�$��0,0,0,10q��� $#��Compound Operation Threshold:��$��SemiBold"��� $ 
Horizontal=�$��0,2,0,5q�.+��� 
 � Minimum$	
 0.5Q� � Maximum$	 5.0Q� �� TickFrequency$	 0.1Q� �� IsSnapToTickEnabled$
 True��$	��200�� � Value �� *#!CompoundOperationThresholdSeconds+�� $��Center=�$��10,0,0,0q����� *#!CompoundOperationThresholdSeconds+ �� StringFormat$ {0:F1}s���� $E��?Operations completed within this time are grouped for Undo/Redo��
�������$
��Wrap=�$��11(�$��0,0,0,15q��� ���� *SelectedStep+ � 	Resources ��%    �� $��2��.@��� .���� ���� *StatusMessage+�� $��Right=�.���� $ 
Horizontal=�.+��� $��Steps:��$��0,0,5,0q��� $
��Bold"����� *Steps.Count+�� $
��10,0q��� $��	Can Undo:��$��0,0,5,0q��� $
��Bold"����� *	CanUndo+�� $
��10,0q��� $��	Can Redo:��$��0,0,5,0q��� $
��Bold"����� *	CanRedo+