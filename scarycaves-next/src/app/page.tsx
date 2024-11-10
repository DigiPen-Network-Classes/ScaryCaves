import Link from 'next/link';
export default function Page() {
    return <div className="text-center">
        <p className="home-title">Before you lies the entrance to ....</p>
        <h4 className="display-4 home-title">
            <Link href='/scarycave' passHref className="home-title">
                The SCARY CAVE
            </Link>
        </h4>
    </div>
};
