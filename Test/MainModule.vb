Imports TkMPQLib

Module MainModule

    Sub Main()
        Dim Rnd As New Random()
        Dim MPQ As New TkMPQ()
        Dim Files() As String
        Dim RS As MPQReader
        Dim File As String
        If True Then
            MPQ.Listfile.AddRange(IO.File.ReadLines("sc.txt"))
            MPQ.OpenMPQ("원본.scx")
            Files = MPQ.Listfile.GetPaths()
            File = Files(Rnd.Next(Files.Length))
            RS = MPQ.GetFile(File, Locale.English)
        Else
            MPQ.Listfile.AddRange(IO.File.ReadLines("w3.txt"))
            MPQ.OpenMPQ("원본.w3x")
            Files = MPQ.Listfile.GetPaths()
            File = Files(Rnd.Next(Files.Length))
            RS = MPQ.GetFile(File, Locale.Korean)
        End If
        Dim FS As New IO.FileStream(IO.Path.GetFileName(File), IO.FileMode.Create)
        RS.WriteTo(FS)
        FS.Flush()
        RS.Close()
        FS.Close()
    End Sub

End Module
