
#ifdef __cplusplus
extern "C" {
#endif

	/**
	 * Causes an assertion via the assert macro.
	 */
	__declspec ( dllexport ) void produces_assert();

	/**
	 * Causes a pure virtual function call.
	 */
	__declspec ( dllexport ) void produces_purecall();

#ifdef __cplusplus
}
#endif
