   M S B A M L   `   `   `  ����K  GDatabaseEditorV3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:DatabaseEditorV3DatabaseEditorV3  5   	   -clr-namespace:SharedLibrarySharedLibrary  5   	   (    "DatabaseEditorV3.UndoChangesWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   0localclr-namespace:DatabaseEditorV3     5   	   .sharedclr-namespace:SharedLibrary     5   	     � WindowStyle$  
ToolWindow=�5	   	    � Title$ Undo Changes��5
   	   $	��300��6   $	��300��6+    � 	Resources 5      !   SharedLibrary.BoolConverter   BoolToLabelConverter   	TrueValue   
FalseValue$   DatabaseEditorV3.TypeConverter  TypeConverter%   DatabaseEditorV3.TableConverter  TableConverter&    DatabaseEditorV3.ColumnConverter  ColumnConverter(   "DatabaseEditorV3.NewValueConverter  NewValueConverter(   "DatabaseEditorV3.OldValueConverter  OldValueConverter��%�   &	        &	 .     &	 A     &	 T     &	 g     &	 z       5   
   $
 Redo��6<   $
 Undo��6M     5   
   6     5   
   6     5   
   6      5   
   6"     5   
   6"   5      .��5      � .+�5   
   &   System.Windows.Controls.DataGrid     
IsReadOnly .    AutoGenerateColumns .     SelectionUnit$
 Cell=�6f   ��6%   �� *UndoCommands+   Columns 5      0  *System.Windows.Controls.DataGridTextColumn  5      ,	  &System.Windows.Controls.DataGridColumn	 	  Header$	 ID��6%   1
  +System.Windows.Controls.DataGridBoundColumn
 
  Binding
 61   ��  �� Path$ Item.ID�  5      $
	 Type��6%   
 63   �� $ .� �� 	Converter# [   5      $	 Table��6%   
 64   �� $ .�# [   5      $	 Column��6%    	  Width5  /System.Windows.Controls.DataGridLengthConverter$	 100 65   
 6A   �� $ .�# [   5      $	 Value��6%   $	 100 64   
 6@   �� $ .�# [   5      $	 	Old Value��6%   $	 100 68   
 6D   �� $ .�# [ 4  .System.Windows.Controls.DataGridTemplateColumn  5         CellTemplate 5      �� 5      .R�5       �� -   5        |�.  ��5"   %   �� *
IsUndone+# [  5#      5%         RowStyle 5'      �� 5(       �� 
TargetType)  #System.Windows.Controls.DataGridRow"  6    �� Triggers 5)      �� 5*       �� Binding 6&   �� $ IsUndone� �� Value True6H   .a�5+      ��  �� Property � �� �� Value
 �����6;   5,      5.      50      51      52      