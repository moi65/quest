﻿<ControlType("list")> _
Public Class ListControl
    Implements IElementEditorControl

    Private m_attributeName As String
    Private m_controller As EditorController
    Private m_data As IEditorData
    Private m_controlData As IEditorControl
    Private m_listItems As New Dictionary(Of String, ListViewItem)

    ' TO DO: Can we make this generic so we can edit lists of any type??
    Private WithEvents m_list As IEditableList(Of String)

    Public ReadOnly Property AttributeName As String Implements IElementEditorControl.AttributeName
        Get
            Return m_attributeName
        End Get
    End Property

    Public ReadOnly Property Control As System.Windows.Forms.Control Implements IElementEditorControl.Control
        Get
            Return Me
        End Get
    End Property

    Public Property Controller As EditorController Implements IElementEditorControl.Controller
        Get
            Return m_controller
        End Get
        Set(value As EditorController)
            m_controller = value
        End Set
    End Property

    Public Event Dirty(sender As Object, args As DataModifiedEventArgs) Implements IElementEditorControl.Dirty

    Public Sub Initialise(controlData As IEditorControl) Implements IElementEditorControl.Initialise
        m_controlData = controlData
        m_attributeName = controlData.Attribute
    End Sub

    Public Sub Populate(data As IEditorData) Implements IElementEditorControl.Populate
        m_data = data
        If data IsNot Nothing Then
            Value = data.GetAttribute(m_attributeName)
        Else
            Value = Nothing
        End If
    End Sub

    Public Sub Save(data As IEditorData) Implements IElementEditorControl.Save

    End Sub

    Public Property Value As Object Implements IElementEditorControl.Value
        Get
            Return m_list
        End Get
        Set(value As Object)
            m_list = DirectCast(value, IEditableList(Of String))
            UpdateList()
        End Set
    End Property

    Private Sub UpdateList()
        lstList.Clear()
        Dim mainColumn As New ColumnHeader
        lstList.Columns.Add(mainColumn)
        mainColumn.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize)
        lstList.HeaderStyle = ColumnHeaderStyle.None

        If m_list IsNot Nothing Then
            For Each item In m_list.Items
                AddListItem(item.Value)
            Next
        End If
    End Sub

    Private Sub AddListItem(item As IEditableListItem(Of String))
        Dim newListViewItem As ListViewItem = lstList.Items.Add(item.Value)
        m_listItems.Add(item.Key, newListViewItem)
    End Sub

    Private Sub RemoveListItem(item As IEditableListItem(Of String))
        lstList.Items.Remove(m_listItems(item.Key))
        m_listItems.Remove(item.Key)
    End Sub

    Private Sub UpdateListItem(item As IEditableListItem(Of String))
        m_listItems(item.Key).Text = item.Value
    End Sub

    Private Sub ListControl_Resize(sender As Object, e As System.EventArgs) Handles Me.Resize
        lstList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize)
    End Sub

    Private Sub cmdAdd_Click(sender As System.Object, e As System.EventArgs) Handles cmdAdd.Click
        Dim result = EditItem(String.Empty)
        If result.Cancelled Then Return

        m_list.Add(result.Result)
    End Sub

    Private Structure EditItemResult
        Public Cancelled As Boolean
        Public Result As String
    End Structure

    Private Function EditItem(input As String) As EditItemResult
        Dim result As New EditItemResult
        Dim inputResult = InputBox(m_controlData.GetString("editprompt"), DefaultResponse:=input)
        result.Cancelled = (inputResult.Length = 0)
        result.Result = inputResult
        Return result
    End Function

    Private Sub m_list_Added(sender As Object, e As EditableListUpdatedEventArgs(Of String)) Handles m_list.Added
        AddListItem(e.UpdatedItem)

        If (e.Source = EditorUpdateSource.User) Then
            lstList.SelectedItems.Clear()
            m_listItems(e.UpdatedItem.Key).Selected = True
            m_listItems(e.UpdatedItem.Key).Focused = True
            m_listItems(e.UpdatedItem.Key).EnsureVisible()
            lstList.Focus()
        End If
    End Sub

    Private Sub m_list_Removed(sender As Object, e As EditableListUpdatedEventArgs(Of String)) Handles m_list.Removed
        RemoveListItem(e.UpdatedItem)
    End Sub

    Private Sub m_list_Updated(sender As Object, e As EditableListUpdatedEventArgs(Of String)) Handles m_list.Updated
        UpdateListItem(e.UpdatedItem)
    End Sub
End Class
