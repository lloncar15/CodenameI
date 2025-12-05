/*
 * HealthConnectBridge.java
 * Unity bridge for Android Health Connect API
 * 
 * This class provides the native Android side of the Health Connect integration.
 * It communicates with Unity via UnitySendMessage.
 */

package com.gimgim.codenamei.healthconnect;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.health.connect.client.HealthConnectClient;
import androidx.health.connect.client.PermissionController;
import androidx.health.connect.client.permission.HealthPermission;
import androidx.health.connect.client.records.StepsRecord;
import androidx.health.connect.client.request.AggregateRequest;
import androidx.health.connect.client.request.ReadRecordsRequest;
import androidx.health.connect.client.response.AggregateResponse;
import androidx.health.connect.client.response.ReadRecordsResponse;
import androidx.health.connect.client.time.TimeRangeFilter;

import com.google.common.util.concurrent.FutureCallback;
import com.google.common.util.concurrent.Futures;
import com.google.common.util.concurrent.ListenableFuture;
import com.unity3d.player.UnityPlayer;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.Executor;
import java.util.concurrent.Executors;

public class HealthConnectBridge {
    
    private static final String TAG = "HealthConnectBridge";
    private static final String UNITY_GAME_OBJECT = "HealthConnectReceiver";
    public static final int REQUEST_CODE_PERMISSIONS = 1001;
    
    private static volatile HealthConnectBridge instance;
    
    private HealthConnectClient healthConnectClient;
    private final Executor executor = Executors.newSingleThreadExecutor();
    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private final Set<String> permissions = new HashSet<>();
    
    private HealthConnectBridge() {
        permissions.add(HealthPermission.getReadPermission(StepsRecord.class));
    }
    
    public static HealthConnectBridge getInstance() {
        if (instance == null) {
            synchronized (HealthConnectBridge.class) {
                if (instance == null) {
                    instance = new HealthConnectBridge();
                }
            }
        }
        return instance;
    }
    
    // ============================================================
    // Static methods for Unity to call via AndroidJavaClass
    // ============================================================
    
    /**
     * Initialize Health Connect
     */
    public static void initialize() {
        getInstance().init();
    }
    
    /**
     * Check if Health Connect is available
     * @return 0 = unavailable, 1 = available, 2 = needs update
     */
    public static int checkAvailability() {
        return getInstance().checkHealthConnectAvailability();
    }
    
    /**
     * Request Health Connect permissions
     */
    public static void requestPermissions() {
        getInstance().requestHealthPermissions();
    }
    
    /**
     * Check if permissions are granted
     */
    public static boolean checkPermissions() {
        return getInstance().hasPermissions();
    }
    
    /**
     * Get steps since a timestamp
     * @param timestampMillis Milliseconds since epoch
     */
    public static void getStepsSince(long timestampMillis) {
        getInstance().queryStepsSince(timestampMillis);
    }
    
    /**
     * Get steps for a date range
     * @param startMillis Start time in milliseconds
     * @param endMillis End time in milliseconds
     */
    public static void getStepsForDateRange(long startMillis, long endMillis) {
        getInstance().queryStepsForRange(startMillis, endMillis);
    }
    
    /**
     * Get steps for today
     */
    public static void getStepsToday() {
        getInstance().queryStepsToday();
    }
    
    /**
     * Open Health Connect settings
     */
    public static void openHealthConnectSettings() {
        getInstance().openSettings();
    }
    
    /**
     * Open Play Store to install Health Connect
     */
    public static void openPlayStoreForHealthConnect() {
        getInstance().openPlayStore();
    }
    
    // ============================================================
    // Implementation methods
    // ============================================================
    
    /**
     * Initialize the Health Connect client
     */
    private void init() {
        Log.d(TAG, "Initializing Health Connect Bridge");
        
        Context context = getContext();
        if (context == null) {
            sendErrorToUnity("NoContext", "Unable to get Android context");
            return;
        }
        
        int availability = HealthConnectClient.getSdkStatus(context);
        
        switch (availability) {
            case HealthConnectClient.SDK_AVAILABLE:
                try {
                    healthConnectClient = HealthConnectClient.getOrCreate(context);
                    Log.d(TAG, "Health Connect client created successfully");
                    sendMessageToUnity("OnHealthConnectInitialized", "true");
                } catch (Exception e) {
                    Log.e(TAG, "Failed to create Health Connect client", e);
                    sendErrorToUnity("InitFailed", e.getMessage() != null ? e.getMessage() : "Unknown error");
                }
                break;
                
            case HealthConnectClient.SDK_UNAVAILABLE:
                Log.w(TAG, "Health Connect SDK is not available on this device");
                sendMessageToUnity("OnHealthConnectInitialized", "false");
                break;
                
            case HealthConnectClient.SDK_UNAVAILABLE_PROVIDER_UPDATE_REQUIRED:
                Log.w(TAG, "Health Connect needs to be installed or updated");
                sendMessageToUnity("OnHealthConnectInitialized", "needsUpdate");
                break;
                
            default:
                Log.w(TAG, "Unknown Health Connect availability status: " + availability);
                sendMessageToUnity("OnHealthConnectInitialized", "false");
                break;
        }
    }
    
    /**
     * Check if Health Connect is available
     * @return 0 = unavailable, 1 = available, 2 = needs update
     */
    private int checkHealthConnectAvailability() {
        Context context = getContext();
        if (context == null) {
            return 0;
        }
        
        int status = HealthConnectClient.getSdkStatus(context);
        
        switch (status) {
            case HealthConnectClient.SDK_AVAILABLE:
                return 1;
            case HealthConnectClient.SDK_UNAVAILABLE_PROVIDER_UPDATE_REQUIRED:
                return 2;
            default:
                return 0;
        }
    }
    
    /**
     * Request Health Connect permissions
     */
    private void requestHealthPermissions() {
        Activity activity = getActivity();
        if (activity == null) {
            sendErrorToUnity("NoActivity", "Unable to get Android activity");
            return;
        }
        
        if (healthConnectClient == null) {
            sendErrorToUnity("NotInitialized", "Health Connect not initialized");
            return;
        }
        
        // Check if we already have permissions
        ListenableFuture<Set<String>> grantedFuture = 
            healthConnectClient.getPermissionController().getGrantedPermissions();
        
        Futures.addCallback(grantedFuture, new FutureCallback<Set<String>>() {
            @Override
            public void onSuccess(Set<String> granted) {
                if (granted.containsAll(permissions)) {
                    Log.d(TAG, "Permissions already granted");
                    sendMessageToUnity("OnPermissionsResult", "true");
                } else {
                    try {
                        Intent intent = PermissionController
                            .createRequestPermissionResultContract()
                            .createIntent(activity, permissions);
                        
                        activity.startActivityForResult(intent, REQUEST_CODE_PERMISSIONS);
                    } catch (Exception e) {
                        Log.e(TAG, "Failed to create permission intent", e);
                        sendErrorToUnity("PermissionRequestFailed", e.getMessage());
                    }
                }
            }
            
            @Override
            public void onFailure(@NonNull Throwable t) {
                Log.e(TAG, "Failed to check permissions", t);
                sendErrorToUnity("PermissionCheckFailed", t.getMessage());
            }
        }, executor);
    }
    
    /**
     * Check if we have the required permissions
     */
    private boolean hasPermissions() {
        if (healthConnectClient == null) {
            return false;
        }
        
        try {
            ListenableFuture<Set<String>> future = 
                healthConnectClient.getPermissionController().getGrantedPermissions();
            
            // Block and wait for result (not ideal but needed for sync method)
            Set<String> granted = future.get();
            return granted.containsAll(permissions);
            
        } catch (Exception e) {
            Log.e(TAG, "Failed to check permissions", e);
            return false;
        }
    }
    
    /**
     * Query steps since a given timestamp
     */
    private void queryStepsSince(long timestampMillis) {
        if (healthConnectClient == null) {
            sendErrorToUnity("NotInitialized", "Health Connect not initialized");
            return;
        }
        
        Instant startTime = Instant.ofEpochMilli(timestampMillis);
        Instant endTime = Instant.now();
        
        queryStepsInternal(startTime, endTime);
    }
    
    /**
     * Query steps for a specific date range
     */
    private void queryStepsForRange(long startMillis, long endMillis) {
        if (healthConnectClient == null) {
            sendErrorToUnity("NotInitialized", "Health Connect not initialized");
            return;
        }
        
        Instant startTime = Instant.ofEpochMilli(startMillis);
        Instant endTime = Instant.ofEpochMilli(endMillis);
        
        queryStepsInternal(startTime, endTime);
    }
    
    /**
     * Query steps for today
     */
    private void queryStepsToday() {
        if (healthConnectClient == null) {
            sendErrorToUnity("NotInitialized", "Health Connect not initialized");
            return;
        }
        
        LocalDate today = LocalDate.now();
        Instant startTime = today.atStartOfDay(ZoneId.systemDefault()).toInstant();
        Instant endTime = Instant.now();
        
        queryStepsInternal(startTime, endTime);
    }
    
    /**
     * Internal method to query steps
     */
    private void queryStepsInternal(Instant startTime, Instant endTime) {
        Set<Object> metrics = new HashSet<>();
        metrics.add(StepsRecord.COUNT_TOTAL);
        
        TimeRangeFilter timeRange = TimeRangeFilter.between(startTime, endTime);
        
        AggregateRequest request = new AggregateRequest(
            metrics,
            timeRange,
            new HashSet<>()  // Empty data origins = all sources
        );
        
        ListenableFuture<AggregateResponse> future = healthConnectClient.aggregate(request);
        
        final long startMillis = startTime.toEpochMilli();
        final long endMillis = endTime.toEpochMilli();
        
        Futures.addCallback(future, new FutureCallback<AggregateResponse>() {
            @Override
            public void onSuccess(AggregateResponse response) {
                try {
                    Long totalSteps = response.get(StepsRecord.COUNT_TOTAL);
                    if (totalSteps == null) {
                        totalSteps = 0L;
                    }
                    
                    JSONObject result = new JSONObject();
                    result.put("success", true);
                    result.put("steps", totalSteps);
                    result.put("startTime", startMillis);
                    result.put("endTime", endMillis);
                    result.put("source", "HealthConnect");
                    
                    Log.d(TAG, "Steps query successful: " + totalSteps);
                    sendMessageToUnity("OnStepsReceived", result.toString());
                    
                } catch (JSONException e) {
                    Log.e(TAG, "Failed to create JSON response", e);
                    sendErrorToUnity("JSONError", e.getMessage());
                }
            }
            
            @Override
            public void onFailure(@NonNull Throwable t) {
                Log.e(TAG, "Failed to query steps", t);
                sendErrorToUnity("QueryFailed", t.getMessage() != null ? t.getMessage() : "Unknown error");
            }
        }, executor);
    }
    
    /**
     * Get detailed step records (not aggregated)
     */
    public void queryStepRecords(long startMillis, long endMillis) {
        if (healthConnectClient == null) {
            sendErrorToUnity("NotInitialized", "Health Connect not initialized");
            return;
        }
        
        Instant startTime = Instant.ofEpochMilli(startMillis);
        Instant endTime = Instant.ofEpochMilli(endMillis);
        
        TimeRangeFilter timeRange = TimeRangeFilter.between(startTime, endTime);
        
        ReadRecordsRequest<StepsRecord> request = new ReadRecordsRequest.Builder<>(StepsRecord.class)
            .setTimeRangeFilter(timeRange)
            .build();
        
        ListenableFuture<ReadRecordsResponse<StepsRecord>> future = 
            healthConnectClient.readRecords(request);
        
        Futures.addCallback(future, new FutureCallback<ReadRecordsResponse<StepsRecord>>() {
            @Override
            public void onSuccess(ReadRecordsResponse<StepsRecord> response) {
                try {
                    List<StepsRecord> records = response.getRecords();
                    JSONArray recordsArray = new JSONArray();
                    
                    for (StepsRecord record : records) {
                        JSONObject recordJson = new JSONObject();
                        recordJson.put("count", record.getCount());
                        recordJson.put("startTime", record.getStartTime().toEpochMilli());
                        recordJson.put("endTime", record.getEndTime().toEpochMilli());
                        recordJson.put("dataOrigin", record.getMetadata().getDataOrigin().getPackageName());
                        recordsArray.put(recordJson);
                    }
                    
                    JSONObject result = new JSONObject();
                    result.put("success", true);
                    result.put("records", recordsArray);
                    result.put("count", records.size());
                    
                    Log.d(TAG, "Retrieved " + records.size() + " step records");
                    sendMessageToUnity("OnStepRecordsReceived", result.toString());
                    
                } catch (JSONException e) {
                    Log.e(TAG, "Failed to create JSON response", e);
                    sendErrorToUnity("JSONError", e.getMessage());
                }
            }
            
            @Override
            public void onFailure(@NonNull Throwable t) {
                Log.e(TAG, "Failed to query step records", t);
                sendErrorToUnity("QueryRecordsFailed", t.getMessage());
            }
        }, executor);
    }
    
    /**
     * Open Health Connect settings
     */
    private void openSettings() {
        Context context = getContext();
        if (context == null) {
            return;
        }
        
        try {
            Intent intent = new Intent(HealthConnectClient.ACTION_HEALTH_CONNECT_SETTINGS);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            context.startActivity(intent);
        } catch (Exception e) {
            Log.e(TAG, "Failed to open Health Connect settings", e);
        }
    }
    
    /**
     * Open Play Store to install/update Health Connect
     */
    private void openPlayStore() {
        Context context = getContext();
        if (context == null) {
            return;
        }
        
        try {
            Intent intent = new Intent(Intent.ACTION_VIEW);
            intent.setData(Uri.parse("https://play.google.com/store/apps/details?id=com.google.android.apps.healthdata"));
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            context.startActivity(intent);
        } catch (Exception e) {
            Log.e(TAG, "Failed to open Play Store", e);
        }
    }
    
    /**
     * Handle permission result from Unity activity
     * Call this from Unity's onActivityResult
     */
    public void onActivityResult(int requestCode, int resultCode) {
        if (requestCode == REQUEST_CODE_PERMISSIONS) {
            boolean hasPerms = hasPermissions();
            sendMessageToUnity("OnPermissionsResult", String.valueOf(hasPerms));
        }
    }
    
    /**
     * Clean up resources
     */
    public void destroy() {
        instance = null;
    }
    
    // ============================================================
    // Helper methods
    // ============================================================
    
    private Context getContext() {
        try {
            Activity activity = UnityPlayer.currentActivity;
            if (activity != null) {
                return activity.getApplicationContext();
            }
        } catch (Exception e) {
            Log.e(TAG, "Failed to get context", e);
        }
        return null;
    }
    
    private Activity getActivity() {
        try {
            return UnityPlayer.currentActivity;
        } catch (Exception e) {
            Log.e(TAG, "Failed to get activity", e);
        }
        return null;
    }
    
    private void sendMessageToUnity(final String methodName, final String message) {
        mainHandler.post(new Runnable() {
            @Override
            public void run() {
                try {
                    UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, methodName, message);
                } catch (Exception e) {
                    Log.e(TAG, "Failed to send message to Unity: " + methodName, e);
                }
            }
        });
    }
    
    private void sendErrorToUnity(String errorCode, String errorMessage) {
        try {
            JSONObject error = new JSONObject();
            error.put("success", false);
            error.put("errorCode", errorCode);
            error.put("errorMessage", errorMessage != null ? errorMessage : "Unknown error");
            sendMessageToUnity("OnHealthConnectError", error.toString());
        } catch (JSONException e) {
            Log.e(TAG, "Failed to create error JSON", e);
        }
    }
}
