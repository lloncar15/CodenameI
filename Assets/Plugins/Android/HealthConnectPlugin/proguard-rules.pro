# ProGuard rules for Health Connect Plugin (Java version)

# Keep all public classes and methods for Unity to call
-keep public class com.gimgim.codenamei.healthconnect.** { *; }

# Keep Health Connect SDK classes
-keep class androidx.health.connect.** { *; }
-keep class androidx.health.platform.** { *; }

# Keep Guava classes
-keep class com.google.common.** { *; }
-dontwarn com.google.common.**

# Keep Unity player class references
-keep class com.unity3d.player.** { *; }

# Keep JSON classes
-keep class org.json.** { *; }

# Keep Java time classes (desugared)
-keep class java.time.** { *; }
