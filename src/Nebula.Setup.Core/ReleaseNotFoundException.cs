using System;

namespace Nebula.Setup.Core;

public sealed class ReleaseNotFoundException() : Exception("No published Nebula release is available for this channel.");
