   M S B A M L   `   `   `  ����O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem  5   	   K GDatabaseEditorV3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:DatabaseEditorV3DatabaseEditorV3 5   	   '   !DatabaseEditorV3.ConnectionWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   6sys&clr-namespace:System;assembly=mscorlib     5   	   0localclr-namespace:DatabaseEditorV3     5   	     � WindowStyle$  
ToolWindow=�5	   	    � SizeToContent$ Height=�5
   	   $	��300��5   	    � Title$ Database Connection��5   	    � Topmost .  � 	Resources 5       �� 
TargetType �� Property �� Value��%�  '��       '�� G     'G� �     '�� �     '�� !    �� 5   
   " ��6   .�5      ��  � ��$ 3q�6'   5      �� 5   
   " ��6   .�5      ��  � ��$ 3q�6'   5      �� 5   
   " G�6   .�5      ��  � ��$ 3q�6'   5      �� 5   
   " ��6   .�5      ��  � ��$ Center=�62   5      �� 5   
   " ��6   .�5      ��  � ��$ 3q�6'   �� 5       � ��$ 60��6)   5      5       .��5!      �  � RowDefinitions 5"   
   �� 5#      $
��Auto �6   �� 5$      $
��Auto �6   .+�5&   
   � $��0��6    5'      �� 5(      $
��Auto �6    �� 5)      $
��Auto �6    �� 5*      $
��Auto �6    �� 5+      $
��Auto �6    	 � ColumnDefinitions	 5-      �� 5.      $
��Auto �6#   �� 5/      $��* �6#   .+�51      �� $��0��6   $��0��6!   $��Server Name:��52      �� 53      $��0��6   $��1��6$   
 �� 
IsEditable
 .  �� Text 54      ��  �� Path$ 
ServerName� �� UpdateSourceTrigger$ 	LostFocus=���56      �� 57       �� Type" ��6   .s�58      ��akc0ss-v051.nmfs.local,1919��6B   59      5;      �� 5<      $��1��6   $��0��6!   $��Authentication:��5=      �� 5>      $��1��6   $��1��6$   $��Use My Windows Login��5?      -  'System.Windows.Input.KeyboardNavigation   	IsTabStop .   d� 	IsChecked 5@      �� $ TrustedConnection�5A      � 5B      $��2��6   $��1��6    {�5C      �� $ NeedPassword� 5D      �� 5E      $
��Auto �6$   �� 5F      $
��Auto �6$   	 5H      �� 5I      $
��Auto �6'   �� 5J      $��* �6'   .+�5L      �� $��0��6   $��0��6%   $��
User Name:��5M      �� 5N      $��0��6   $��1��6'   ��5O      �� $ UserName��� 5P      $��1��6   $��0��6%   $��	Password:��5Q      G� 5R      -   5        $��1��5R      $��1��6+   5S      5T      �� 5U      $��3��6   $��0��6!   $��	Database:��5V      �� 5W      $��3��6   $��1��6$   ��5X      �� $ ListDatabase� �� SelectedValue 5Y      �� $ Database�5Z      � 5[   
   $��1��6   	 5\      �� 5]      $
��Auto �6#   �� 5^      $��* �6#   �� 5_      $
��Auto �6#   .+�5a      �� -   5        $��0��5a       �� 	IsDefault . $��OK��5c      {�5e      �� $ IsValid�5f      �� 5g      $��2��6    �� IsCancel . $��Cancel��5i      5j      5k      5l      