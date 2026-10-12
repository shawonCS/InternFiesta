// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";

// Global toast notifications
document.addEventListener("DOMContentLoaded", function () {
    if (typeof bootstrap === "undefined") return;

    document.querySelectorAll(".internfiesta-toast")
        .forEach(function (element) {
            bootstrap.Toast.getOrCreateInstance(element, {
                animation: true,
                autohide: true,
                delay: 6500
            }).show();
        });
});

// Save scroll position after form confirmation
document.addEventListener("submit", function (event) {
    const form = event.target;

    if (!(form instanceof HTMLFormElement) ||
        !form.matches("[data-preserve-scroll]")) {
        return;
    }

    queueMicrotask(function () {
        if (event.defaultPrevented) return;

        try {
            sessionStorage.setItem(
                "internfiesta-scroll:" + window.location.pathname,
                JSON.stringify({
                    y: window.scrollY,
                    savedAt: Date.now()
                })
            );
        } catch (error) {
            console.warn("Scroll save failed:", error);
        }
    });
});

// Restore scroll position
window.addEventListener("load", function () {
    const key = "internfiesta-scroll:" + window.location.pathname;

    try {
        const stored = sessionStorage.getItem(key);
        if (!stored) return;

        sessionStorage.removeItem(key);

        const position = JSON.parse(stored);

        if (!Number.isFinite(position.y) ||
            !Number.isFinite(position.savedAt) ||
            Date.now() - position.savedAt > 120000) {
            return;
        }

        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                window.scrollTo(0, position.y);
            });
        });
    } catch (error) {
        console.warn("Scroll restore failed:", error);
    }
});