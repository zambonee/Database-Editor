   M S B A M L   `   `   `  ����K  GDatabaseEditorV3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:DatabaseEditorV3DatabaseEditorV3  5   	   *    $DatabaseEditorV3.PreviewImportWizard   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   0localclr-namespace:DatabaseEditorV3     5   	     � Title$  Preview Imported Data��5   	   $	��450��6'   $	��800��64    � 	Resources 5	       �� 
TargetType �� Property �� Value��%  '��       '�� G     '� �     �� 5
   
   " ��6   .�5      ��  � ��$ 3q�6'   5      �� 5   
   " ��6   .�5      ��  � ��$ 3q�6'   �� 5       � ��$ 70��6)   5      �� 5   
   " �6   .�5      ��  � ��$ 3q�6'   5      5      .��5      �  � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   &   System.Windows.Controls.DataGrid  -   5        $��0��5         AutoGenerateColumns .    SelectionUnit$ CellOrRowHeader=�5!       �� AlternationCount$ 2��5"      	   AlternatingRowBackground
	 �������5      �� *CollectionPreview+
   RowStyle
 5$      �� 5%      )  #System.Windows.Controls.DataGridRow"  6   .�5&      ��  �@�ToolTip 60   �� *ErrorMessage+�� 5'       � ��
 �  ��63   �� 5(       � ��
 �����63    �� Triggers 5)      �� 5*       �� Binding 6&   �� *ErrorMessage+ �� Value 6G   Z� .a�5+      ��  � ��
 �����6;   �� 5,       � ��
 �   �6;   5-      5/      51      �� 52   
   $��1��6   $��Update existing rows��6     � ToolTip$� �Update instead of inserting a row when the Primary Key value here matches the value in the database. Rows without a Primary Key value are inserted as new rows.��55       d� 	IsChecked 53      �� *UpdateOnExists+ �� Mode$ OneWayToSource=�{�54      �� *HasPrimaryKey+55      �� 56   
   $��2��6   $��Create new parent records��6    $r lCreate a new record in the parent table when no match can be made given the parent table column values here.��59       57      �� *InsertOnNotExists+$ OneWayToSource=�{�58      �� *HasReferencedTable+59      � 5:   
   $��3��6    � ColumnDefinitions 5;      �� 5<      $
��Auto �6#   �� 5=      $
��Auto �6#   �� 5>      $��* �6#   �� 5?      $
��Auto �6#   .+�5A      �� -   5        $��0��5A      $
��Test��6%   64   �� 5B      -   5        $��1��5B      $
��Save��6%   64   �� 5C      $��3��6   $��Close��6%    �� IsCancel . 65   5D      5E      5F      