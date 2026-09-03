#nullable disable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal sealed class DirectComputeKernel : IDisposable
{
    private IntPtr device, context, shader, parameters, layouts, output, staging, parameterView, layoutView, outputView;
    private readonly Update update;
    private readonly Dispatch dispatch;
    private readonly Copy copy;
    private readonly Map map;
    private readonly Unmap unmap;
    internal string AdapterName { get; }

    internal DirectComputeKernel(byte[] bytecode)
    {
        try
        {
            Check(D3D11CreateDevice(IntPtr.Zero, 1, IntPtr.Zero, 0,
                new[] { 0xb000 }, 1, 7, out device, out _, out context));
            AdapterName = ReadAdapterName();
            GCHandle code = GCHandle.Alloc(bytecode, GCHandleType.Pinned);
            try
            {
                Check(Function<CreateShader>(device, 18)(device, code.AddrOfPinnedObject(),
                    (UIntPtr)bytecode.Length, IntPtr.Zero, out shader));
            }
            finally { code.Free(); }
            Function<SetShader>(context, 69)(context, shader, IntPtr.Zero, 0);
            update = Function<Update>(context, 48);
            dispatch = Function<Dispatch>(context, 41);
            copy = Function<Copy>(context, 47);
            map = Function<Map>(context, 14);
            unmap = Function<Unmap>(context, 15);
        }
        catch { Dispose(); throw; }
    }

    internal void Configure(int[] snapshot, int layoutInts, int resultInts)
    {
        ReleaseBuffers();
        parameters = Buffer(snapshot.Length, 0, 8, 0, 0x40, 4);
        layouts = Buffer(layoutInts, 0, 8, 0, 0x40, 4);
        output = Buffer(resultInts, 0, 0x80, 0, 0x40, 4);
        staging = Buffer(resultInts, 3, 0, 0x20000, 0, 0);
        Check(Function<CreateView>(device, 7)(device, parameters, IntPtr.Zero, out parameterView));
        Check(Function<CreateView>(device, 7)(device, layouts, IntPtr.Zero, out layoutView));
        Check(Function<CreateView>(device, 8)(device, output, IntPtr.Zero, out outputView));
        Function<SetViews>(context, 67)(context, 0, 2, new[] { parameterView, layoutView });
        Function<SetOutput>(context, 68)(context, 0, 1, new[] { outputView }, IntPtr.Zero);
        Upload(parameters, snapshot);
    }

    internal static byte[] Compile(string source)
    {
        byte[] code = Encoding.UTF8.GetBytes(source);
        IntPtr blob = IntPtr.Zero, errors = IntPtr.Zero;
        try
        {
            int hr = D3DCompile(code, (UIntPtr)code.Length, null, IntPtr.Zero, IntPtr.Zero,
                "Solve", "cs_5_0", 1 << 15, 0, out blob, out errors);
            if (hr < 0) throw new InvalidOperationException(errors == IntPtr.Zero ? "Shader compilation failed" :
                Marshal.PtrToStringAnsi(Function<BlobPointer>(errors, 3)(errors)));
            var result = new byte[checked((int)Function<BlobSize>(blob, 4)(blob).ToUInt64())];
            Marshal.Copy(Function<BlobPointer>(blob, 3)(blob), result, 0, result.Length);
            return result;
        }
        finally { Release(ref errors); Release(ref blob); }
    }

    internal void Run(int[] candidates, int[] results, int groups)
    {
        Upload(layouts, candidates);
        dispatch(context, (uint)groups, 1, 1);
        copy(context, staging, output);
        Check(map(context, staging, 0, 1, 0, out Mapped data));
        try { Marshal.Copy(data.Data, results, 0, results.Length); }
        finally { unmap(context, staging, 0); }
    }

    private void Upload(IntPtr buffer, int[] values)
    {
        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);
        try { update(context, buffer, 0, IntPtr.Zero, handle.AddrOfPinnedObject(), 0, 0); }
        finally { handle.Free(); }
    }

    private IntPtr Buffer(int length, uint usage, uint bind, uint access, uint misc, uint stride)
    {
        var desc = new BufferDescription
        {
            ByteWidth = checked((uint)length * 4),
            Usage = usage,
            BindFlags = bind,
            CpuAccess = access,
            Misc = misc,
            Stride = stride
        };
        Check(Function<CreateBuffer>(device, 3)(device, ref desc, IntPtr.Zero, out IntPtr buffer));
        return buffer;
    }

    private string ReadAdapterName()
    {
        Guid id = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
        IntPtr dxgi = IntPtr.Zero, adapter = IntPtr.Zero, description = Marshal.AllocHGlobal(304);
        try
        {
            Check(Marshal.QueryInterface(device, ref id, out dxgi));
            Check(Function<GetAdapter>(dxgi, 7)(dxgi, out adapter));
            Check(Function<GetDescription>(adapter, 8)(adapter, description));
            return Marshal.PtrToStringUni(description, 128).TrimEnd('\0');
        }
        finally { Marshal.FreeHGlobal(description); Release(ref adapter); Release(ref dxgi); }
    }

    public void Dispose()
    {
        Release(ref context);
        ReleaseBuffers();
        Release(ref shader); Release(ref device);
    }

    private void ReleaseBuffers()
    {
        Release(ref outputView); Release(ref layoutView); Release(ref parameterView);
        Release(ref staging); Release(ref output); Release(ref layouts); Release(ref parameters);
    }

    // D3D11 COM method slots and blittable layouts match the Windows SDK d3d11.h boundary.
    private static T Function<T>(IntPtr instance, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));
    private static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }
    private static void Release(ref IntPtr value) { if (value != IntPtr.Zero) { Marshal.Release(value); value = IntPtr.Zero; } }
    [StructLayout(LayoutKind.Sequential)] private struct BufferDescription { internal uint ByteWidth, Usage, BindFlags, CpuAccess, Misc, Stride; }
    [StructLayout(LayoutKind.Sequential)] private struct Mapped { internal IntPtr Data; internal uint RowPitch, DepthPitch; }
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int CreateBuffer(IntPtr self, ref BufferDescription desc, IntPtr initial, out IntPtr buffer);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int CreateView(IntPtr self, IntPtr resource, IntPtr desc, out IntPtr view);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int CreateShader(IntPtr self, IntPtr code, UIntPtr size, IntPtr linkage, out IntPtr shader);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void SetShader(IntPtr self, IntPtr shader, IntPtr instances, uint count);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void SetViews(IntPtr self, uint start, uint count, [In] IntPtr[] views);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void SetOutput(IntPtr self, uint start, uint count, [In] IntPtr[] views, IntPtr counts);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void Update(IntPtr self, IntPtr resource, uint subresource, IntPtr box, IntPtr data, uint rowPitch, uint depthPitch);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void Dispatch(IntPtr self, uint x, uint y, uint z);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void Copy(IntPtr self, IntPtr destination, IntPtr source);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Map(IntPtr self, IntPtr resource, uint subresource, uint mode, uint flags, out Mapped mapped);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate void Unmap(IntPtr self, IntPtr resource, uint subresource);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate IntPtr BlobPointer(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate UIntPtr BlobSize(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetAdapter(IntPtr self, out IntPtr adapter);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetDescription(IntPtr self, IntPtr description);
    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int D3D11CreateDevice(IntPtr adapter, uint driver,
        IntPtr software, uint flags, [In] int[] levels, uint levelCount, uint sdk, out IntPtr device, out int level, out IntPtr context);
    [DllImport("d3dcompiler_47.dll", ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern int D3DCompile(
        [In] byte[] source, UIntPtr size, string name, IntPtr defines, IntPtr include, string entry, string target,
        uint flags, uint effectFlags, out IntPtr code, out IntPtr errors);
}
