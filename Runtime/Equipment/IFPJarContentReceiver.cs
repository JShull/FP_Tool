// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    /// <summary>Receiver implementations store accepted content and return an amount within [0, offeredMl].</summary>
    public interface IFPJarContentReceiver
    {
        void OnJarEntered(FP_JarContent jar);
        void OnJarExited(FP_JarContent jar);
        /// <summary>Store the accepted portion. Do not invoke external callbacks or mutate the source here.</summary>
        float ReceiveJarContent(FP_JarContent source, string contentId, float offeredMl);
        /// <summary>Source debit has committed. Publish receiver feedback here.</summary>
        void OnJarContentReceived(FP_JarContent source, string contentId, float acceptedMl);
    }
}
