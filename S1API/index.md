---
_layout: landing
---

<style>
/* Landing Page Custom Styles */
body {
    position: relative;
    overflow-x: hidden;
    background: #0a0a0a;
}

/* Animated Background Canvas */
.animated-bg {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: -1;
    overflow: hidden;
    background: linear-gradient(135deg, #0a0a0a 0%, #1a1a1a 100%);
}

.border-top {
    border-top-color: #49505700 !important;
}

/* Animated Gradient Overlays */
.gradient-orb {
    position: absolute;
    border-radius: 50%;
    filter: blur(80px);
    opacity: 0.3;
    animation: float 12s ease-in-out infinite;
}

.gradient-orb:nth-child(1) {
    width: 500px;
    height: 500px;
    background: radial-gradient(circle, #8B6F47 0%, transparent 70%);
    top: -200px;
    left: -200px;
    animation-delay: 0s;
}

.gradient-orb:nth-child(2) {
    width: 400px;
    height: 400px;
    background: radial-gradient(circle, #4A7C59 0%, transparent 70%);
    bottom: -150px;
    right: -150px;
    animation-delay: 5s;
}

.gradient-orb:nth-child(3) {
    width: 350px;
    height: 350px;
    background: radial-gradient(circle, #6B8E6A 0%, transparent 70%);
    top: 50%;
    right: 10%;
    animation-delay: 10s;
}

/* Floating Grid Overlay */
.grid-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-image: 
        linear-gradient(rgba(139, 111, 71, 0.03) 1px, transparent 1px),
        linear-gradient(90deg, rgba(139, 111, 71, 0.03) 1px, transparent 1px);
    background-size: 80px 80px;
    animation: slideGrid 18s linear infinite;
}

@keyframes slideGrid {
    0% {
        transform: translate(0, 0);
    }
    100% {
        transform: translate(80px, 80px);
    }
}

/* Floating Particles */
.particle {
    position: absolute;
    border-radius: 50%;
    background: rgba(139, 111, 71, 0.1);
    animation: drift 15s linear infinite;
}

.particle:nth-child(4) {
    width: 4px;
    height: 4px;
    top: 20%;
    left: 10%;
    animation-delay: 0s;
}

.particle:nth-child(5) {
    width: 6px;
    height: 6px;
    top: 40%;
    right: 15%;
    animation-delay: 3s;
}

.particle:nth-child(6) {
    width: 3px;
    height: 3px;
    bottom: 30%;
    left: 20%;
    animation-delay: 7s;
}

.particle:nth-child(7) {
    width: 5px;
    height: 5px;
    top: 60%;
    left: 50%;
    animation-delay: 11s;
}

.particle:nth-child(8) {
    width: 4px;
    height: 4px;
    bottom: 20%;
    right: 25%;
    animation-delay: 15s;
}

@keyframes float {
    0%, 100% {
        transform: translate(0, 0) scale(1);
    }
    33% {
        transform: translate(30px, -50px) scale(1.1);
    }
    66% {
        transform: translate(-20px, 30px) scale(0.9);
    }
}

@keyframes drift {
    0% {
        transform: translate(0, 0);
        opacity: 0;
    }
    10% {
        opacity: 1;
    }
    90% {
        opacity: 1;
    }
    100% {
        transform: translate(100px, -100px);
        opacity: 0;
    }
}

.landing-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 4rem 2rem;
    min-height: 75vh;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    text-align: center;
    position: relative;
    z-index: 1;
}

/* Hero Section */
.hero-section {
    width: 100%;
    position: relative;
}

.hero-title {
    font-size: 4rem;
    font-weight: 700;
    margin-bottom: 1rem;
    line-height: 1.2;
    text-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
    animation: fadeInUp 0.8s ease-out;
}

.gradient-text {
    background: linear-gradient(135deg, #8B6F47 0%, #4A7C59 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
    animation: gradientShift 5s ease-in-out infinite;
    background-size: 200% 200%;
}

@keyframes gradientShift {
    0%, 100% {
        background-position: 0% 50%;
    }
    50% {
        background-position: 100% 50%;
    }
}

@keyframes fadeInUp {
    from {
        opacity: 0;
        transform: translateY(30px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.hero-subtitle {
    font-size: 1.5rem;
    color: #999;
    margin-bottom: 2rem;
    font-weight: 400;
    text-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
    animation: fadeInUp 0.8s ease-out 0.2s both;
}

.hero-description {
    font-size: 1.2rem;
    color: #777;
    max-width: 800px;
    margin: 0 auto;
    line-height: 1.6;
    margin-bottom: 2rem;
    text-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
    animation: fadeInUp 0.8s ease-out 0.4s both;
}

/* Dark Mode Support */
@media (prefers-color-scheme: dark) {
    .hero-subtitle {
        color: #999;
    }
    
    .hero-description {
        color: #777;
    }
}

/* Responsive Design */
@media (max-width: 768px) {
    .hero-title {
        font-size: 2.5rem;
    }
    
    .hero-subtitle {
        font-size: 1.2rem;
    }
    
    .hero-description {
        font-size: 1rem;
    }
    
    .landing-container {
        padding: 2rem 1rem;
    }
    
    .gradient-orb {
        filter: blur(60px);
    }
}
</style>

<!-- Animated Background -->
<div class="animated-bg">
    <!-- Gradient Orbs -->
    <div class="gradient-orb"></div>
    <div class="gradient-orb"></div>
    <div class="gradient-orb"></div>
    
    <!-- Grid Overlay -->
    <div class="grid-overlay"></div>
    
    <!-- Floating Particles -->
    <div class="particle"></div>
    <div class="particle"></div>
    <div class="particle"></div>
    <div class="particle"></div>
    <div class="particle"></div>
</div>

<div class="landing-container">

<div class="hero-section">
<h1 class="hero-title">
<span class="gradient-text">S1API</span> Documentation
</h1>

<p class="hero-subtitle">A Schedule One Mono / IL2CPP Cross Compatibility Layer</p>

<p class="hero-description">S1API provides a unified API for developing mods that work across both Mono and IL2CPP runtimes in Schedule One.</p>

</div>

</div>
