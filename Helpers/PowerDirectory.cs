﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ActiveDs;

namespace PowerBaseWpf.Helpers
{
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("274FAE1F-3626-11D1-A3A4-00C04FB950DC")]
    [TypeLibType(2)]
    [ComImport]
    public class NameTranslateClass : IADsNameTranslate, NameTranslate
    {
        [DispId(1)]
        public extern int ChaseReferral { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] set; }

        
        //[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        //public extern void NameTranslateClass();

        [DispId(2)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public extern void Init([In] int lnSetType, [MarshalAs(UnmanagedType.BStr), In] string bstrADsPath);

        [DispId(3)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public extern void InitEx([In] int lnSetType, [MarshalAs(UnmanagedType.BStr), In] string bstrADsPath, [MarshalAs(UnmanagedType.BStr), In] string bstrUserID, [MarshalAs(UnmanagedType.BStr), In] string bstrDomain, [MarshalAs(UnmanagedType.BStr), In] string bstrPassword);

        [DispId(4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public extern void Set([In] int lnSetType, [MarshalAs(UnmanagedType.BStr), In] string bstrADsPath);

        [DispId(5)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public extern string Get([In] int lnFormatType);

        [DispId(6)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public extern void SetEx([In] int lnFormatType, [MarshalAs(UnmanagedType.Struct), In] object pVar);

        [DispId(7)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public extern object GetEx([In] int lnFormatType);
    }
    [CoClass(typeof(NameTranslateClass))]
    [Guid("B1B272A3-3625-11D1-A3A4-00C04FB950DC")]
    [ComImport]
    public interface NameTranslate : ActiveDs.IADsNameTranslate
    {
    }
    [Guid("B1B272A3-3625-11D1-A3A4-00C04FB950DC")]
    [TypeLibType(4160)]
    [ComImport]
    public interface IADsNameTranslate
    {
        [DispId(1)]
        int ChaseReferral { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] set; }

        [DispId(2)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Init([In] int lnSetType, [MarshalAs(UnmanagedType.BStr), In] string bstrADsPath);

        [DispId(3)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void InitEx([In] int lnSetType, [MarshalAs(UnmanagedType.BStr), In] string bstrADsPath, [MarshalAs(UnmanagedType.BStr), In] string bstrUserID, [MarshalAs(UnmanagedType.BStr), In] string bstrDomain, [MarshalAs(UnmanagedType.BStr), In] string bstrPassword);

        [DispId(4)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Set([In] int lnSetType, [MarshalAs(UnmanagedType.BStr), In] string bstrADsPath);

        [DispId(5)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Get([In] int lnFormatType);

        [DispId(6)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetEx([In] int lnFormatType, [MarshalAs(UnmanagedType.Struct), In] object pVar);

        [DispId(7)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Struct)]
        object GetEx([In] int lnFormatType);
    }
}
