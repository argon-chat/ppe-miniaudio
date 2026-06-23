// Generates C# P/Invoke bindings for the ma_wrap flat C ABI.
//
//   bindings/ma_wrap.h  --bindgen-->  Rust FFI  --csbindgen-->  NativeMethods.g.cs
//
// Run with:  cargo build     (set LIBCLANG_PATH to your LLVM bin dir on Windows)
//
// The Rust FFI file is only an intermediate for csbindgen; it is never compiled
// into this crate, so there is no link-time dependency on the native library.
use std::path::PathBuf;

fn main() {
    let manifest = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
    let header = manifest.join("../../bindings/ma_wrap.h");
    let ffi_out = manifest.join("src/ma_wrap_ffi.rs");
    let cs_out = manifest.join("../Miniaudio/NativeMethods.g.cs");

    println!("cargo:rerun-if-changed={}", header.display());

    // 1) bindgen: parse the (tiny, standalone) wrapper header into Rust FFI.
    bindgen::Builder::default()
        .header(header.to_string_lossy())
        .allowlist_function("ma_wrap_.*")
        .allowlist_type("ma_.*_handle")
        .generate()
        .expect("bindgen failed to parse ma_wrap.h")
        .write_to_file(&ffi_out)
        .expect("failed to write Rust FFI");

    // 2) csbindgen: Rust FFI -> C# DllImport bindings.
    csbindgen::Builder::default()
        .input_bindgen_file(&ffi_out)
        .csharp_dll_name("miniaudio")
        .csharp_namespace("Miniaudio.Native")
        .csharp_class_name("NativeMethods")
        .csharp_entry_point_prefix("")
        .csharp_method_prefix("")
        .generate_csharp_file(&cs_out)
        .expect("csbindgen failed to generate C#");
}
