import Link from 'next/link'
const Navbar: React.FC = () => {
    return  (
    <nav className=" navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
        <div className="container-fluid">
            <Link href="/" className="navbar-brand" passHref>The Scary Cave</Link><br/>
            <div className="navbar d-sm-inline-flex justfy-content-between" id="navbarNav">
                <ul className="navbar-nav flex-grow-1">
                    <li className="nav-item">
                        <Link href='/startover' className="nav-link" passHref>
                            Start Over
                        </Link>
                    </li>
                    <li>
                        <Link href="/logout" className="nav-link" passHref>
                            Logout
                        </Link>
                    </li>
                    <li className="nav-item">
                        <Link href="/about" passHref className="nav-link">
                            About
                        </Link>
                    </li>
                    <li className="nav-item">
                        <a href="https://github.com/DigiPen-Network-Classes/ScaryCaves">Project</a>
                    </li>
                </ul>
            </div>
        </div>
    </nav>
);
};
export default Navbar;
