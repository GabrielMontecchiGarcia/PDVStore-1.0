import type { SVGProps } from 'react';

// ============================================================================
// Conjunto de ícones em SVG (stroke), sem dependência externa.
// ============================================================================

export type IconName =
  | 'dashboard' | 'cart' | 'box' | 'layers' | 'users' | 'truck' | 'shopping'
  | 'cash' | 'userCog' | 'info' | 'logout' | 'search' | 'plus' | 'close'
  | 'edit' | 'trash' | 'print' | 'download' | 'menu' | 'alert' | 'check'
  | 'lock' | 'arrowLeft' | 'refresh' | 'minus' | 'receipt' | 'eye';

const paths: Record<IconName, string[]> = {
  dashboard: ['M3 3h7v7H3z', 'M14 3h7v4h-7z', 'M14 11h7v10h-7z', 'M3 14h7v7H3z'],
  cart: ['M3 4h2l2.4 10.2a2 2 0 0 0 2 1.8h7.7a2 2 0 0 0 2-1.6L21 8H6', 'M9 20a1 1 0 1 0 0-2 1 1 0 0 0 0 2z', 'M18 20a1 1 0 1 0 0-2 1 1 0 0 0 0 2z'],
  box: ['M21 8.5 12 3 3 8.5v7L12 21l9-5.5z', 'M3 8.5 12 14l9-5.5', 'M12 14v7'],
  layers: ['M12 3 3 8l9 5 9-5z', 'M3 12l9 5 9-5', 'M3 16l9 5 9-5'],
  users: ['M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2', 'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', 'M22 21v-2a4 4 0 0 0-3-3.9', 'M16 3.1a4 4 0 0 1 0 7.8'],
  truck: ['M1 4h14v12H1z', 'M15 8h4l3 4v4h-7z', 'M5.5 20a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3z', 'M18.5 20a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3z'],
  shopping: ['M6 2 2 6v14a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V6l-4-4z', 'M2 6h20', 'M16 10a4 4 0 0 1-8 0'],
  cash: ['M2 6h20v12H2z', 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z', 'M6 9h.01', 'M18 15h.01'],
  userCog: ['M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2', 'M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', 'M19.6 11.5l1.4.8-1 1.7-1.4-.6a3.6 3.6 0 0 1-1 .6l-.2 1.5h-2l-.2-1.5a3.6 3.6 0 0 1-1-.6l-1.4.6-1-1.7 1.4-.8a3.6 3.6 0 0 1 0-1.2l-1.4-.8 1-1.7 1.4.6a3.6 3.6 0 0 1 1-.6l.2-1.5h2l.2 1.5a3.6 3.6 0 0 1 1 .6l1.4-.6 1 1.7-1.4.8a3.6 3.6 0 0 1 0 1.2z'],
  info: ['M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z', 'M12 16v-4', 'M12 8h.01'],
  logout: ['M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4', 'M16 17l5-5-5-5', 'M21 12H9'],
  search: ['M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16z', 'M21 21l-4.3-4.3'],
  plus: ['M12 5v14', 'M5 12h14'],
  minus: ['M5 12h14'],
  close: ['M18 6 6 18', 'M6 6l12 12'],
  edit: ['M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7', 'M18.5 2.5a2.1 2.1 0 0 1 3 3L12 15l-4 1 1-4z'],
  trash: ['M3 6h18', 'M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2', 'M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6', 'M10 11v6', 'M14 11v6'],
  print: ['M6 9V2h12v7', 'M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2', 'M6 14h12v8H6z'],
  download: ['M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4', 'M7 10l5 5 5-5', 'M12 15V3'],
  menu: ['M3 6h18', 'M3 12h18', 'M3 18h18'],
  alert: ['M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z', 'M12 9v4', 'M12 17h.01'],
  check: ['M20 6 9 17l-5-5'],
  lock: ['M4 11h16v11H4z', 'M8 11V7a4 4 0 0 1 8 0v4'],
  arrowLeft: ['M19 12H5', 'M12 19l-7-7 7-7'],
  refresh: ['M23 4v6h-6', 'M1 20v-6h6', 'M3.5 9a9 9 0 0 1 14.9-3.4L23 10', 'M1 14l4.6 4.4A9 9 0 0 0 20.5 15'],
  receipt: ['M4 2h16v20l-3-2-3 2-3-2-3 2-3-2z', 'M8 7h8', 'M8 11h8', 'M8 15h5'],
  eye: ['M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z', 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z']
};

interface IconProps extends SVGProps<SVGSVGElement> {
  name: IconName;
  size?: number;
}

export function Icon({ name, size = 20, ...props }: IconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      {...props}
    >
      {paths[name].map((d, indice) => (
        <path key={indice} d={d} />
      ))}
    </svg>
  );
}
