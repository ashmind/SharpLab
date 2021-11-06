Imports System.Collections.Generic
Public Module Program
    Public Sub Main(ByVal args() As String)
        Dim list As New List(Of Integer)
        list.Sort(Function(a, b) a.CompareTo(b))
    End Sub
End Module