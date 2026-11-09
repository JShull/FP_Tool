// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    /// <summary>Destination for a tool's portion. Accept/store first, publish feedback only after the tool debit commits.</summary>
    public interface IFPToolContentReceiver
    {
        void OnContentToolEntered(FP_JarContentTool tool);
        void OnContentToolExited(FP_JarContentTool tool);
        float ReceiveToolContent(FP_JarContentTool tool, FP_JarContent sourceJar, string contentId, float offeredMl);
        void OnToolContentReceived(FP_JarContentTool tool, FP_JarContent sourceJar, string contentId, float acceptedMl);
    }
}
