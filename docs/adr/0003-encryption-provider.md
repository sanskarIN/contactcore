# ADR 0003: Fail-closed optional database encryption

Status: Accepted

ContactCore does not bundle deprecated or unsupported SQLCipher binaries. The infrastructure can run against a SQLCipher-compatible SQLite provider. When a key is requested, the runtime verifies `cipher_version`; lack of cipher support is an error. This preserves a real encryption option without creating a false sense of protection or pinning an abandoned binary package.
