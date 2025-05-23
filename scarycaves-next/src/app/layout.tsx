import type { Metadata, Viewport } from "next";
import "bootstrap/dist/css/bootstrap.css";
import "./globals.css";
import BootstrapClient from '@/components/BootstrapClient';
import Navbar from '@/components/Navbar';

export const metadata: Metadata = {
  title: "The Scary Cave",
  description: "Do you dare enter ... The Scary Cave?",
};
export const viewport: Viewport = {
    width: "device-width",
    initialScale: 1.0,
}
export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
  <html lang="en">
  <head>
    <meta charSet="utf-8"/>
  </head>
  <body>
  <Navbar />
  <div className={"background-wrapper"}></div>
  <div className="container" >
    <main role="main" className="pb-3">
      {children}
    </main>
  </div>
  <footer className="border-top footer text-muted">
    <div className="container">
      &copy; 2024 - <a href="https://www.linkedin.com/in/tonyrasa/">Tony Rasa</a>. All rights reserved.
    </div>
  </footer>
  <BootstrapClient/>
  </body>
  </html>
  );
}
