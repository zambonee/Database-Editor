   M S B A M L   `   `   `  ����K  GDatabaseEditorV3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:DatabaseEditorV3DatabaseEditorV3  5   	   -clr-namespace:SharedLibrarySharedLibrary  5   	   #    DatabaseEditorV3.ImportWizard   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   0localclr-namespace:DatabaseEditorV3     5   	   .sharedclr-namespace:SharedLibrary     5   	   -   5          � Title$  Import from File��5	   	   $	��450��6"   $	��400��6/    J� CommandBindings 5      )  #System.Windows.Input.CommandBinding  5   
   -   5           Command$% ApplicationCommands.NotACommand��5      6C    � 	Resources 5      !   SharedLibrary.BoolConverter   BoolToVisibilityConverter   	TrueValue   
FalseValue+   %SharedLibrary.PathToFileNameConverter  PathConverter+   %SharedLibrary.NullableColumnConverter  NullablePairConverter �� 
TargetType �� Property �� Value  MainCollection	 �� Source
 �� Path��%�  &	        &	 6     &	 I     '�� \     '�� �     '�� �     &	 1      5   
   $ Visible��6A   $ 	Collapsed��6U     5   
   6)     5   
   6)   �� 5   
   " ��6   .�5      ��  � ��$ 1q�6'   5      �� 5   
   " ��6   .�5      ��  � ��$ 1q�6'   5      �� 5   
   " ��6   .�5      ��  � ��$ 1q�6'   5      �� 5   
   	 66   �� $
 SelectedTable.Columns�5      .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5       $��* �6   �� 5!      $
��Auto �6   .+�5#   
   � $��0��6   $��3q�5$       5%      �� 5&      $
��Auto �6    �� 5'      $
��Auto �6    �� 5(      $
��Auto �6    �� 5)      $
��Auto �6    �� 5*      $
��Auto �6    �� 5+      $
��Auto �6     � ColumnDefinitions 5-      �� 5.      $
��Auto �6#   �� 5/      $��* �6#   �� 50      $
��Auto �6#   .+�52      �� $��0��6   $��0��53      $��
File Name:��54      �� 55      $��0��6   $��1��56       �� 
IsReadOnly . ��58      �� *
FileName+ �� 	Converter# [ �� 59      -   5        $��0��59      $��2��5:      $
��Find��5;      5<      �� 5=      $��1��6   $��0��5>      $��Connection String:��5?      y�5@      �� $
 ConnectionStringVisible�# [  �� 5A      $��1��6   $��1��5B      ��5C      �� *ConnectionString+y�5D      �� $
 ConnectionStringVisible�# [  �� 5E      $��2��6   $��0��5F      $��Worksheet/Table:��5G      �� 5H      $��2��6   $��1��5I      ��5J      �� *ListWorksheets+ �� SelectedValue 5K      �� *	Worksheet+�� 5L      $��3��6   $��0��5M      $��Filter:��5N      �� 5O      $��3��6   $��1��5P      ��5Q      �� *Filter+�� 5R      $��4��6   $��0��5S      $��Database Schema:��5T      �� 5U      $��4��6   $��1��5V       �� DisplayMemberPath$	 Key��5W       �� SelectedValuePath$ Value��5X      ��5Y      �� *DatabaseStructure+ 5Z      �� *CollectionSchemaTables+�� 5[      $��5��6   $��0��5\      $��Database Table:��5]      �� 5^      $��5��6   $��1��5_      $ DisplayName��5`      ��5a      �� $
 CollectionSchemaTables� �� SelectedItem 5b      �� $
 SelectedTable�5c      &   System.Windows.Controls.DataGrid  5d   
   $��1��6      AutoGenerateColumns .     
IsReadOnly .    EnableRowVirtualization .  ��5g      �� $
 CollectionMatches�   Columns 5i      0  *System.Windows.Controls.DataGridTextColumn  5j      ,  &System.Windows.Controls.DataGridColumn   Header$ File Column��6%      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 5l   %   1	  +System.Windows.Controls.DataGridBoundColumn 	  Binding 5k   %   �� $
 
FileColumn�5l   %   4
  .System.Windows.Controls.DataGridTemplateColumn
  5m      $ Find in Parent��6)   $ * 5n   )    
  CellTemplate 5o      �� 5p      .R�5q      �  5r   "   �� 5s   &   $
��Auto �67   �� 5t   &   $��* �67   .+�5v   "   �� $��0��6+    d� 	IsChecked 5w   +   �� $
 FindInForeign� �� UpdateSourceTrigger$ PropertyChanged=��� 5x   "   $��1��6+   $ DisplayName��5y   +   ��5z   +   �� $,
 &DataContext.SelectedTable.ParentTables� �� RelativeSource ��  �� AncestorType" � 5{   +   �� *DatabaseTable+$ PropertyChanged=�{�5|   +   �� $
 FindInForeign�5}      5~      5�      
  5�      $ Database Column��6)   $ * 6B    5�      �� 5�      .R�5�      ]� .+�5�   "   �� $��Right=�6)   $��X��6@   $%��ApplicationCommands.NotACommand��6L    � ToolTip$ Clear match��6�   ��6v   �� 6�   �� 5�   "   -   5        $ Display��5�   +    5�   '   �� $
 DatabaseColumn�$ PropertyChanged=�  �� TargetNullValue$  ignore��# [ ��5�   &   �� 5�   *   " ��60   ! �� BasedOn#! [B��6F   .�5�   .   ��  � �� 5�   2   -  'System.Windows.Data.CompositeCollection  5�   6   �� 5�   :   " �� 
Collection" 6N   �� # �� Source## [ 5�   7   5�   /   $ �� Triggers$ 5�   .   �� 5�   2   % �� Binding% 6>   �� $
 FindInForeign�& �� Value& True6e   .a�5�   6   ��  � �� 6T   �� $
 DatabaseTable.Columns�5�   3   5�   +   5�   #   5�      5�      5�      5�      � 5�   
   $��2��6   $��3q�6    5�      ��%R   '��       �� 5�      " ��6   .�5�      ��  � ��$ 75��61   5�      5�       5�      �� 5�      $
��Auto �6#   �� 5�      $��* �6#   �� 5�      $
��Auto �6#   .+�5�      �� -   5        $��0��5�      $��Preview��6%   67   �� 5�      -   5        $��2��5�      $��Close��6%   ' �� IsCancel' . 6E   5�      5�      5�      