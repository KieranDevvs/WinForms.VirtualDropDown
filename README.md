# VirtualDropDownList

![VirtualDropDownList Control](./VirtualDropDownList.png)

## Overview

**VirtualDropDownList** is a high-performance UI control library designed to handle massive datasets with ease. In standard dropdown controls, rendering thousands of items can lead to significant memory overhead and UI lagging. This library utilizes **UI Virtualization** to solve that problem.

### Purpose
The primary goal of this library is to provide a seamless user experience by only rendering the elements currently visible in the viewport. Whether you are displaying 100 items or 1,000,000, the memory footprint remains constant and the scrolling stays buttery smooth.

---

## Key Features

* **High Performance:** Efficiently manages large-scale lists using element recycling.
* **Low Memory Footprint:** Only creates UI objects for the visible area, regardless of total list size.
* **Custom Templates:** Easily bind and style your data items to match your application's UI.
* **Instant Search:** Optimized logic for filtering through large collections without freezing the main thread.
* **Keyboard Navigation:** Full support for arrow keys, Enter, and "type-to-select" functionality.

---

## Installation

Clone and build the assembly.
TODO: provide a nuget package.