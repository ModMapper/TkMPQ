using System;

/// <summary>Tk MPQ Library</summary>
namespace TkMPQLib
{
    /// <summary>
    /// Tk MPQ Library Created by ModMapper
    /// Compatible .Net Framework 4.0 (Windows XP or Older)
    /// e-Mail : modmapper@tkyuki.kr
    /// </summary>
    public static class TkMPQInfo
    {
        /// <summary>Tk MPQ Library Version</summary>
        public static readonly string Version;

        /// <summary>Tk MPQ Library Discription</summary>
        public const string Discription =
            "Tk MPQ Library Created by ModMapper\r\n" +
            "Compatible .Net Framework 4.0 (Windows XP or Older)\r\n" +
            "e-Mail : modmapper@tkyuki.kr";

        static TkMPQInfo() {
            Version Ver = typeof(TkMPQInfo).Assembly.GetName().Version;
            Version = $"{Ver.Major}.{Ver.Minor}.{String.Format("{0:X}{1:X}", Ver.Build & 0xF, Ver.Revision & 0xF)}";
        }

        // Tk MPQ Library
        // MPQ를 읽거나 작성하는 닷넷 MPQ라이브러리 입니다.
        // 닷넷 프레임워크 4.0 (Windows XP 이상)에서 작동합니다.
        // 제작 : ModMapper (modmapper@tkyuki.kr)
        // 홈페이지 : http://blog.tkyuki.kr
    }
}
