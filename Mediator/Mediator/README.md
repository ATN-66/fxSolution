*Recommended Markdown Viewer: [Markdown Editor](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor2)*

## Getting Started

Browse and address `TODO:` comments in `View -> Task List` to learn the codebase and understand next steps for turning the generated code into production code.

Explore the [WinUI Gallery](https://www.microsoft.com/store/productId/9P3JFPWWDZRC) to learn about available controls and design patterns.

Relaunch Template Studio to modify the project by right-clicking on the project in `View -> Solution Explorer` then selecting `Add -> New Item (Template Studio)`.

## Publishing

For projects with MSIX packaging, right-click on the application project and select `Package and Publish -> Create App Packages...` to create an MSIX package.

For projects without MSIX packaging, follow the [deployment guide](https://docs.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps) or add the `Self-Contained` Feature to enable xcopy deployment.

## CI Pipelines

See [README.md](https://github.com/microsoft/TemplateStudio/blob/main/docs/WinUI/pipelines/README.md) for guidance on building and testing projects in CI pipelines.

## Changelog

See [releases](https://github.com/microsoft/TemplateStudio/releases) and [milestones](https://github.com/microsoft/TemplateStudio/milestones).

## Feedback

Bugs and feature requests should be filed at https://aka.ms/templatestudio.

--------------------------------------------------------------------------
PipeMethodCalls.MessagePack is part of the PipeMethodCalls library, which is a lightweight .NET Standard 2.0 library designed to facilitate method calls over named and anonymous pipes for inter-process communication (IPC). It supports two-way communication with callbacks​1​.

The PipeMethodCalls.MessagePack specifically is a pre-built serializer option that uses the MessagePack-CSharp serializer. The MessagePack-CSharp serializer is known for its excellent performance. This is used to serialize the data for transmission over the IPC mechanism​1​.

Here are some of the key features of the PipeMethodCalls library:

It supports 100% asynchronous communication with .ConfigureAwait(false) to minimize context switches and reduce thread use.
It's quite compact at 45KB and has no built-in dependencies.
It can invoke async methods.
It supports passing and returning complex types with pluggable JSON or binary serialization.
It allows interleaved or multiple simultaneous calls.
It supports throwing exceptions.
It provides CancellationToken support.
It works on Windows, Linux, and MacOS​1​.
However, there are a few things that PipeMethodCalls does not support:

Methods with out and ref parameters.
Properties.
Method overloads​1​.

PipeMethodCalls.MessagePack is an extension of the PipeMethodCalls library that provides a specific serialization method using the MessagePack format. It's not designed to work independently of PipeMethodCalls, but rather as a component that PipeMethodCalls can use to handle serialization and deserialization of data.

In the context of PipeMethodCalls, serialization is the process of converting complex data types into a format that can be transmitted over IPC mechanisms, and deserialization is the reverse process of converting the transmitted data back into complex data types. The PipeMethodCalls.MessagePack library provides an implementation of these processes using the MessagePack format, which is known for its high performance.

So, while you could potentially use the PipeMethodCalls.MessagePack library independently in the sense that you could use it to serialize and deserialize data using the MessagePack format, it's intended to be used as part of the PipeMethodCalls library to handle serialization and deserialization for IPC. If you're not using PipeMethodCalls for IPC, there might be more direct ways to use MessagePack serialization in your project.
Yes, that's correct. The PipeMethodCalls library is used to facilitate method calls over inter-process communication (IPC) channels, like named or anonymous pipes. It enables you to remotely invoke methods and receive the results of those method calls.

The PipeMethodCalls.MessagePack is a specific implementation of a serializer for the PipeMethodCalls library. The job of the serializer is to convert the method parameters and results into a format that can be transmitted over the IPC channel. In this case, PipeMethodCalls.MessagePack uses the MessagePack format for this serialization.

So, when you make a method call with PipeMethodCalls, the method parameters are serialized using the PipeMethodCalls.MessagePack serializer (or another serializer if you're using a different one), sent over the IPC channel, then deserialized on the other end. The method is then executed, and the results are serialized and sent back over the IPC channel, where they are deserialized and returned to your code.

This allows PipeMethodCalls to pass complex types as parameters and return values, even though the IPC channel itself can only transmit raw data.

---------------------------------------------------------------------------





