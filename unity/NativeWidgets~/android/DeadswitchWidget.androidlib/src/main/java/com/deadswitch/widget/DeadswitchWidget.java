package com.deadswitch.widget;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.res.Resources;
import android.widget.RemoteViews;

/**
 * DEADSWITCH home-screen widget (doc 08 s5): threat level, the hottest faction's heat and the next forecast timer,
 * as the game wrote them when the handler left (WidgetBridge.cs, SharedPreferences "deadswitch_widget"). Tapping it
 * opens the game. Resources are looked up by name so the library needs no generated R class.
 */
public class DeadswitchWidget extends AppWidgetProvider {
    /** Called by the game after it writes a new snapshot. */
    public static void refresh(Context context) {
        AppWidgetManager manager = AppWidgetManager.getInstance(context);
        int[] ids = manager.getAppWidgetIds(new ComponentName(context, DeadswitchWidget.class));
        for (int id : ids) {
            update(context, manager, id);
        }
    }

    @Override
    public void onUpdate(Context context, AppWidgetManager manager, int[] ids) {
        for (int id : ids) {
            update(context, manager, id);
        }
    }

    private static void update(Context context, AppWidgetManager manager, int id) {
        Resources res = context.getResources();
        String pkg = context.getPackageName();
        int layout = res.getIdentifier("ds_widget", "layout", pkg);
        if (layout == 0) {
            return;
        }

        SharedPreferences prefs = context.getSharedPreferences("deadswitch_widget", Context.MODE_PRIVATE);
        RemoteViews views = new RemoteViews(pkg, layout);
        views.setTextViewText(res.getIdentifier("ds_threat", "id", pkg), prefs.getString("threat", "OPEN THE TERMINAL"));
        views.setTextViewText(res.getIdentifier("ds_heat", "id", pkg), prefs.getString("heat", ""));
        views.setTextViewText(res.getIdentifier("ds_next", "id", pkg), prefs.getString("next", ""));

        Intent launch = context.getPackageManager().getLaunchIntentForPackage(pkg);
        if (launch != null) {
            PendingIntent open = PendingIntent.getActivity(context, 0, launch, PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
            views.setOnClickPendingIntent(res.getIdentifier("ds_root", "id", pkg), open);
        }

        manager.updateAppWidget(id, views);
    }
}
